module ECS

open System
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Content

//components
type Position = { x : float; y : float; }
let defaultPosition = {x = 0.0; y = 0.0;}

type Velocity = { x : float; y : float; }
let defaultVelocity = {x = 0.0; y = 0.0;}

type Appearance = { texture: Texture2D; }

//end components

type World = {
    entities : Set<string>;

    position: Map<string, Position>;
    velocity: Map<string, Velocity>;
    appearance: Map<string, Appearance>;
    }


let createEntity id world =
    {world with entities = Set.add id world.entities}

let destroyEntity id world =
    {world with entities = Set.remove id world.entities}


let addPosition id pos world  =
    {world with position = Map.add id pos world.position}
let getPosition id world = Map.tryFind id world.position

let addVelocity id vel world  =
    {world with velocity = Map.add id vel world.velocity}
let getVelocity id world = Map.tryFind id world.velocity

let addAppearance id textureName (contentManager:ContentManager) world  =
    {world with appearance = Map.add id (contentManager.Load textureName) world.appearance}
let getAppearance id world = Map.tryFind id world.appearance


//systems

//updates entities with position and velocity
let runMovement dt world =
    let advance (pos:Position) vel = ({x = pos.x + (dt * vel.x); y = pos.y + (dt * vel.y);} : Position)
    let maybe = new FSharpx.Option.MaybeBuilder()
    let nextPosition id = maybe 
                            {
                                let! pos = getPosition id world
                                let! vel = getVelocity id world
                                return advance pos vel
                            }
    let updatePos id pos =  if Set.contains id world.entities && Option.isSome (nextPosition id) then
                                Option.get (nextPosition id)
                            else
                                pos

    let nextPositions = Map.map updatePos world.position
                                                     
    {world with position = nextPositions}

let createPaddle id pos world =
    createEntity id world |>
    addPosition id defaultPosition |>
    addVelocity id defaultVelocity

let createBall id pos world =
    createEntity id world |>
    addPosition id defaultPosition |>
    addVelocity id { x = 1.0; y = 0.0; }