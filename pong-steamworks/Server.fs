module Server

open Microsoft.Xna.Framework

open SharedServerClient


type World = {
    entities : Set<string>;

    position: Map<string, Position>;
    velocity: Map<string, Velocity>;
    }

let defaultWorld = {
    entities = Set.empty;
    position = Map.empty;
    velocity = Map.empty;
    }

let createEntity id world =
    {world with entities = Set.add id world.entities}

let destroyEntity id world =
    let ({   
            entities = entities;
            position = position;
            velocity = velocity; 
        }:World) = world
    {   
        entities = Set.remove id entities;
        position = Map.remove id position;
        velocity = Map.remove id velocity;
    }

let addPosition id pos world  =
    {world with position = Map.add id pos world.position}

let addVelocity id vel world  =
    {world with velocity = Map.add id vel world.velocity}


//  SYSTEMS

//updates entities with position and velocity
let runMovement (dt:float) world =
    let advance (pos:Position) (vel:Vector2) = ( pos + (float32 dt * vel) : Position)
    let updatePos id pos =  
        let position = Map.tryFind id world.position
        let velocity = Map.tryFind id world.velocity

        if Set.contains id world.entities && Option.isSome position && Option.isSome velocity then
            advance (Option.get position) (Option.get velocity)
        else
            pos

    let nextPositions = Map.map updatePos world.position
                                                     
    {world with position = nextPositions}

