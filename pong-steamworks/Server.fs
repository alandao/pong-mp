module Server

open Microsoft.Xna.Framework
open System.Collections.Generic

open HelperFunctions
open SharedServerClient


type World = {
    entities : HashSet<string>;

    position: Dictionary<string, Position>;
    velocity: Dictionary<string, Velocity>;
    }

let defaultWorld = {
    entities = HashSet<string>();
    position = Dictionary<string, Position>();
    velocity = Dictionary<string, Velocity>();
    }

let destroyEntity id world = 
    world.position.Remove(id) |> ignore
    world.velocity.Remove(id) |> ignore
    world.entities.Remove(id) |> ignore

//  SYSTEMS

//updates entities with position and velocity
let runMovement (dt:float) world =
    let advance (pos:Position) (vel:Vector2) = ( pos + (float32 dt * vel) : Position)

    let entities = List<string>(world.position.Keys)
    for id in entities do
        let velocity = tryFind id world.velocity

        if Option.isSome velocity then
            world.position.[id] <- advance world.position.[id] (Option.get velocity) 

