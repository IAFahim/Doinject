﻿using System.Linq;
using System.Threading.Tasks;
using Mew.Core.TaskHelpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Doinject
{
    public class GameObjectContext : MonoBehaviour, IInjectableComponent, IContext, IGameObjectContextRoot
    {
        private IContext ParentContext { get; set; }

        public Scene Scene => Context.Scene;
        public GameObject ContextObject => gameObject;
        public Context Context { get; private set; }
        public SceneContextLoader OwnerSceneContextLoader => ParentContext.SceneContextLoader;
        public SceneContextLoader SceneContextLoader { get; private set; }
        public GameObjectContextLoader GameObjectContextLoader { get; private set; }
        public IContextArg Arg { get; private set; } = new NullContextArg();

        private bool Initializing { get; set; }
        public bool Initialized { get; private set; }

        private async Task Initialize()
        {
            if (Initializing || Initialized) return;
            Initializing = true;

            ParentContext = FindParentContext();
            Context = new Context(gameObject, ParentContext?.Context);
            Context.Container.Bind<IContextArg>().FromInstance(Arg);
            SceneContextLoader = gameObject.AddComponent<SceneContextLoader>();
            SceneContextLoader.SetContext(this);
            GameObjectContextLoader = gameObject.AddComponent<GameObjectContextLoader>();
            GameObjectContextLoader.SetContext(this);

            ParentContext?.GameObjectContextLoader.Register(this);

            await InstallBindings();
            await Context.Container.GenerateResolvers();
            await InjectIntoUnderContextObjects();

            Initializing = false;
            Initialized = true;
        }

        public void SetArgs(IContextArg arg)
        {
            Arg = arg ?? new NullContextArg();
        }

        private async void Start()
        {
            while (!Initializing && !Initialized)
            {
                await TaskHelper.NextFrame();
                if (this) await Initialize();
            }
        }

        private IContext FindParentContext()
        {
            if (transform.parent)
            {
                var parentContext = transform.parent.GetComponentInParent<GameObjectContext>();
                if (parentContext) return parentContext;
            }

            if (SceneContext.TryGetSceneContext(gameObject.scene, out var sceneContext))
                return sceneContext;

            if (ProjectContext.Instance)
                return ProjectContext.Instance;

            return null;
        }

        private GameObjectContext[] FindChildContexts()
        {
            return transform.GetComponentsInChildren<GameObjectContext>(true)
                .Where(x => x != this)
                .Where(x => x.transform.parent && x.transform.parent.GetComponentInParent<GameObjectContext>() == this)
                .ToArray();
        }

        private async void OnDestroy()
        {
            if (Context is null) return;
            if (SceneContextLoader) await SceneContextLoader.DisposeAsync();
            if (GameObjectContextLoader) await GameObjectContextLoader.DisposeAsync();
            await Context.DisposeAsync();
            ParentContext?.GameObjectContextLoader.Unregister(this);
            if (gameObject) Destroy(gameObject);
        }

        private async Task InstallBindings()
        {
            if (!Initialized) await Initialize();
            Context.Container.Bind<IContext>().FromInstance(this);
            Context.Container.BindFromInstance(SceneContextLoader);
            Context.Container.BindFromInstance(GameObjectContextLoader);
            var installers = GetComponentsUnderContext<IBindingInstaller>();
            Context.Install(installers, Arg);
        }

        private async Task InjectIntoUnderContextObjects()
        {
            var targets = GetComponentsUnderContext<IInjectableComponent>()
                .Where(x => x.enabled);
            await Task.WhenAll(targets.Select(x
                => Context.Container.InjectIntoAsync(x).AsTask()));
        }

        private T[] GetComponentsUnderContext<T>()
        {
            var targetType = typeof(T);
            return transform.GetComponentsInChildren(targetType, true)
                .Where(x => x.GetComponentInParent<GameObjectContext>() == this)
                .Where(x => x != this)
                .Cast<T>()
                .ToArray();
        }

        public void Dispose()
        {
            if (this) Destroy(this);
        }
    }
}