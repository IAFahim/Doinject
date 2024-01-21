﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Doinject.Context
{
    public class SceneContext : MonoBehaviour, IContext
    {
        private static readonly ConcurrentDictionary<Scene, SceneContext> SceneContextMap = new();

        public static bool TryGetSceneContext(Scene scene, out SceneContext sceneContext)
        {
            return SceneContextMap.TryGetValue(scene, out sceneContext);
        }

        public GameObject ContextObject => gameObject;
        public Context Context { get; private set; }
        public SceneContextLoader OwnerSceneContextLoader { get; set; }
        public SceneContextLoader SceneContextLoader { get; private set; }
        public GameObjectContextLoader GameObjectContextLoader { get; private set; }
        public IContextArg Arg { get; set; } = new NullContextArg();

        public Scene Scene => Context.Scene;


        public async Task Initialize(Scene scene, IContext parentContext, SceneContextLoader sceneContextLoader)
        {
            Context = new Context(scene, parentContext?.Context);
            Context.Container.Bind<IContextArg>().FromInstance(Arg);
            if (GetComponentsUnderContext<SceneContext>().Any(x => x != this))
                throw new InvalidOperationException("Do not place SceneContext statically in scene.");
            OwnerSceneContextLoader = sceneContextLoader;
            SceneContextLoader = gameObject.AddComponent<SceneContextLoader>();
            SceneContextLoader.SetContext(this);
            GameObjectContextLoader = gameObject.AddComponent<GameObjectContextLoader>();
            GameObjectContextLoader.SetContext(this);
            SceneContextMap[scene] = this;

            InstallBindings();
            await Context.Container.GenerateResolvers();
            await InjectIntoUnderContextObjects();
        }

        public void SetArgs(IContextArg arg)
        {
            Arg = arg ?? new NullContextArg();
        }

        private async void OnDestroy()
        {
            if (SceneContextLoader) await SceneContextLoader.DisposeAsync();
            if (GameObjectContextLoader) await GameObjectContextLoader.DisposeAsync();
            var scene = Context.Scene;
            await Context.DisposeAsync();
            if (OwnerSceneContextLoader) await OwnerSceneContextLoader.UnloadAsync(this);
            SceneContextMap.Remove(scene, out _);
        }

        private void InstallBindings()
        {
            Context.Container.Bind<IContext>().FromInstance(this);
            Context.Container.BindFromInstance(SceneContextLoader);
            Context.Container.BindFromInstance(GameObjectContextLoader);
            var targets = GetComponentsUnderContext<IBindingInstaller>();
            foreach (var component in targets)
                component.Install(Context.Container, Arg);
        }

        private async Task InjectIntoUnderContextObjects()
        {
            var targets = GetComponentsUnderContext<IInjectableComponent>();
            await Task.WhenAll(targets.Select(x => Context.Container.InjectIntoAsync(x).AsTask()));
        }

        private IEnumerable<T> GetComponentsUnderContext<T>()
        {
            return Scene.FindComponentsByType(typeof(T))
                .Where(x =>
                {
                    if (x is GameObjectContext)
                    {
                        var parent = x.transform.parent;
                        return !parent || !parent.GetComponentInParent<GameObjectContext>();
                    }
                    return !x.GetComponentInParent<GameObjectContext>();
                })
                .Cast<T>();
        }

        public void Dispose()
        {
            if (this) Destroy(this);
        }
    }
}