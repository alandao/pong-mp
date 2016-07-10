module SharedServerClient

open Microsoft.Xna.Framework
open System.Collections.Generic

open Components

type Entity = System.Guid

let CreateEntity() = System.Guid.NewGuid()

type EntityComponentDictionary =
    | Position of Dictionary<Entity, Position>
    | Velocity of Dictionary<Entity, Velocity>
    | Appearance of Dictionary<Entity, Appearance>

let EntityComponentRemove id dict =
    match dict with
    | Position dict -> dict.Remove(id)
    | Velocity dict -> dict.Remove(id)
    | Appearance dict -> dict.Remove(id)

type ComponentStore = Dictionary<System.Type, EntityComponentDictionary>

let EntityAddComponent id comp (componentStore:Dictionary<System.Type, EntityComponentDictionary>) =
    componentStore.[comp.GetType()]
    //TODO: Finish add function
let DestroyEntity id (componentStore:Dictionary<System.Type, EntityComponentDictionary>) =
    for comp in componentStore do
        EntityComponentRemove id comp.Value |> ignore


type PlayerInput = 
    | PaddleUp of bool
    | PaddleDown of bool

type ClientToServerMsg = 
    | PaddleAt of x : float * y : float


//  Server Message types
let msg_snapshot = 0

