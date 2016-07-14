module SharedServerClient

open Microsoft.Xna.Framework
open System.Collections.Generic

open HelperFunctions
open ECS
open ECSTypes


//Systems

//updates entities with position and velocity
let private RunMovement (dt:float) (posComponents:Dictionary<Entity, Position>) velComponents =
    let advance (pos:Position) (vel:Velocity) = ( pos + (float32 dt * vel) : Position)

    let entities = List<Entity>(posComponents.Keys)
    for entID in entities do
        let velocity = DictionaryX.TryFind entID velComponents

        if Option.isSome velocity then
            posComponents.[entID] <- advance posComponents.[entID] (Option.get velocity) 



type PlayerInput = 
    | PaddleUp of bool
    | PaddleDown of bool

type ClientToServerMsg = 
    | PaddleAt of x : float * y : float


//  Server Message types
type ServerMessage =
    | Snapshot = 0
    | Schema = 1

