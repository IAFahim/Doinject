# Doinject
Asynchronous DI Container for Unity

![Logo.svg](Writerside%7E/images/Logo.svg)

![](https://img.shields.io/badge/unity-2022.3%20or%20later-green?logo=unity)
![](https://img.shields.io/badge/unity-2023.2%20or%20later-green?logo=unity)
[![](https://img.shields.io/badge/license-MIT-blue)](https://github.com/mewlist/MewAssets/blob/main/LICENSE)

## ドキュメント

* [日本語ドキュメント](https://mewlist.github.io/Doinject)
* [Documents in English](https://mewlist.github.io/Doinject/en/introduction-en.html)


## サンプルプロジェクト

* [サンプルプロジェクト](https://github.com/mewlist/DoinjectExample)

## インストール

### Unity Package Manager でインストール

以下の順にパッケージをインストールしてください。

```
https://github.com/mewlist/MewCore.git
```

```
https://github.com/mewlist/Doinject.git
```

# Doinject について

Doinject は、Unity 向けの非同期 DI(Dependency Injection) フレームワークです。

非同期DIコンテナというコンセプトが起点となっています。
Unity 2022 LTS / 2023.2 以降をサポートします。

## コンセプト

### 非同期DIコンテナ

非同期なインスタンスの生成と解放をフレームワーク側でサポートします。
これにより、Addressables Asset Systems を通したインスタンスも扱うことができます。
また、カスタムファクトリを自分で作れば、時間のかかるインスタンスの生成を任せてしまうこともできます。

### Unity のライフサイクルと矛盾しないコンテクスト空間

Unity のライフサイクルと矛盾しない形でコンテクスト空間を定義するように設計されています。
シーンを閉じれば、シーンに紐づいたコンテクストを閉じ、そのコンテクスト空間で作成されたインスタンスが消え、
コンテクストを持つゲームオブジェクトを Destroy すれば、同様にコンテクストを閉じます。
コンテクスト空間はフレームワークによって自動的に構成され、複数のコンテクストがロードされた場合には、親子関係を作ります。

### Addressable Asset System との連携

Addressable Asset System のインスタンスも扱うことができ、ロードハンドルの解放を自動化することができます。
Addressables のリソース管理は、独自のリソースマネジメントシステムを作ったりと、慎重な実装が必要となりますが、
Doinject を使うと、Addressables のロード・解放を勝手にやってくれます。

### 平易な記述

ファクトリパターン、(コンテクストに閉じた)シングルトンパターン、サービスロケーターパターンの置き換えを、平易な記述で実現することができます。
また、カスタムファクトリやカスタムリゾルバを作ることで、より複雑なインスタンス生成シナリオに対応することができます。

## バインド記述

### 型バインディング

| 記述                                                         | リゾルバの動作　                                | 提供タイプ     |
|------------------------------------------------------------|-----------------------------------------|-----------|
| ```container.Bind<SomeClass>();```                         | ```new SomeClass()```                   | cached    |
| ```container.Bind<SomeClass>().AsSingleton();```　          | ```new SomeClass()```                   | singleton |
| ```container.Bind<SomeClass>().AsTransient();```　          | ```new SomeClass()```                   | transient |
| ```container.Bind<SomeClass>().Args(123,"ABC");```　        | ```new SomeClass(123, "abc")```         | cached    |
| ```container.Bind<ISomeInterface>().To<SomeClass>();```　   | ```new SomeClass() as ISomeInterface``` | cached    |
| ```container.Bind<ISomeInterface, SomeClass>();```　        | ```new SomeClass() as ISomeInterface``` | cached    |
| ```container.Bind<SomeClass>()```<br />```.FromInstance(instance);```  | ```instance```                          | instance  |
| ```container.BindInstance(instance);```                    | ```instance```                          | instance  |

### MonoBehaviour バインディング

| 記述                                                                  | リゾルバの動作　                                                                                                                |
|---------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|
| ```container.Bind<SomeComponent>();```                              | ```new GameObject().AddComponent<SomeComponent>()```                                                                    |
| ```container```<br />```.Bind<SomeComponent>()```<br />```.Under(transform);``` | ```var instance = new GameObject().AddComponent<SomeComponent>();```<br/>```instance.transform.SetParent(transform);``` |
| ```container```<br />```.Bind<SomeComponent>()```<br />```.On(gameObject);```   | ```gameObject.AddComponent<SomeComponent>()```                                                                             |
| ```container```<br />```.BindPrefab<SomeComponent>(somePrefab);```  | ```Instantiate(somePrefab).GetComponent<SomeComponent>()```                                                             |

### Addressables バインディング


| 記述                                                                                         | リゾルバの動作　                                                                                                                                                                                      　             |
|--------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ```container```<br />```.BindAssetReference<SomeAddressalbesObject>(assetReference);```    | ```var handle = Addressables```<br />```.LoadAssetAsync<GameObject>(assetReference)```<br/><br/>```await handle.Task```　                                                                                    |
| ```container```<br />```.BindPrefabAssetReference<SomeComponent>(prefabAssetReference);``` | ```var handle = Addressables```<br />```.LoadAssetAsync<GameObject>(prefabAssetReference)```<br/><br/>```var prefab = await handle.Task```<br/><br/>```Instantiate(prefab).GetComponent<SomeComponent>()``` |
| ```container```<br />```.BindAssetRuntimeKey<SomeAddressalbesObject>("guid or path");```    | ```var handle = Addressables```<br />```.LoadAssetAsync<GameObject>("guid or path")```<br/><br/>```await handle.Task```　                                                                                    |
| ```container```<br />```.BindPrefabAssetRuntimeKey<SomeComponent>("guid or path");```      | ```var handle = Addressables```<br />```.LoadAssetAsync<GameObject>("guid or path")```<br/><br/>```var prefab = await handle.Task```<br/><br/>```Instantiate(prefab).GetComponent<SomeComponent>()```       |

### ファクトリバインディング

| 記述                                                                                      | インスタンス (仮想コード)                                                                                                                                              |
|-----------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ```container```<br />```.Bind<SomeClass>()```<br />```.AsFactory();```                  | ```var resolver = new TypeResolver<SomeClass>()```<br/><br/>```new Factory<SomeClass>(resolver) as IFactory<SomeClass>```                                   |
| ```container```<br />```.Bind<SomeComponent>()```<br />```.AsFactory();```              | ```var resolver = new MonoBehaviourResolver<SomeComponent>()```<br/><br/>```new Factory<SomeComponent>(resolver))```<br />``` as IFactory<SomeComponent>``` |
| ```container```<br />```.Bind<SomeClass>()```<br />```.AsCustomFactory<MyFactory>();``` | ```new CustomFactoryResolver<MyFactory>() as IFactory<SomeClass>```                                                                          |


## インジェクション記述

### インストーラー

```
public class SomeInstaller : BindingInstallerScriptableObject
{
    public override void Install(DIContainer container, IContextArg contextArg)
    {
        container.Bind<SomeClass>();
    }
}
```

### コンストラクタインジェクション

```
class ExampleClass
{
    // Constructor Injection
    public ExampleClass(SomeClass someClass)
    { ... }
}
```

### メソッドインジェクション

```
class ExampleClass
{
    // Method Injection
    [Inject]
    public Construct(SomeClass someClass)
    { ... }
}
```

### MonoBehaviour へのインジェクション

```
// Inherits IInjectableComponent
class ExampleComponent : MonoBehaviour, IInjectableComponent
{
    // Method Injection
    [Inject]
    public void Construct(SomeClass someClass)
    { ... }
}
```
