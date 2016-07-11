module EntitySystems

open Components
open System.Collections.Generic

open HelperFunctions

type Entity = System.Guid

type ComponentStore = Dictionary<ComponentType, Dictionary<Entity, Component>>

let GetComponent id compType (componentStore : ComponentStore) =
    let (>>=) m f = Option.bind f m
    DictionaryX.TryFind compType componentStore
    >>= DictionaryX.TryFind id   

let EntityAddComponent id comp (componentStore : ComponentStore) =
    let compType = ComponentToComponentType comp
    if not <| componentStore.ContainsKey(compType) then
        componentStore.Add(compType, new Dictionary<Entity, Component>())
    componentStore.[compType].Add(id, comp)

let CreateEntity () = System.Guid.NewGuid()

let KillEntity id (componentStore : ComponentStore) =
    for typeCompPair in componentStore do
        let comp = typeCompPair.Value
        comp.Remove id |> ignore
