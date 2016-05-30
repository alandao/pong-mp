module ECS

open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Content

//components
type Position = Vector2
let defaultPosition = Vector2(0.f, 0.f)

type Velocity = Vector2
let defaultVelocity = Vector2(0.f, 0.f)

type Appearance = { texture : Texture2D; size : Vector2 }

type World = {
    entities : Set<string>;

    position: Map<string, Position>;
    velocity: Map<string, Velocity>;
    appearance: Map<string, Appearance>;
    }
let defaultWorld = {
    entities = Set.empty;
    position = Map.empty;
    velocity = Map.empty;
    appearance = Map.empty;
    }

let createEntity id world =
    {world with entities = Set.add id world.entities}

//fix memory leak here once components are done
let destroyEntity id {  entities = entities;
                        position = position;
                        velocity = velocity;
                        appearance = appearance; } =

    {   entities = Set.remove id entities;
        position = Map.remove id position;
        velocity = Map.remove id velocity;
        appearance = Map.remove id appearance; }


let addPosition id pos world  =
    {world with position = Map.add id pos world.position}
let getPosition id world = Map.tryFind id world.position

let addVelocity id vel world  =
    {world with velocity = Map.add id vel world.velocity}
let getVelocity id world = Map.tryFind id world.velocity

let addAppearance id textureName (contentManager:ContentManager) world  =
    {world with appearance = Map.add id {texture = contentManager.Load<Texture2D> textureName; 
                                         size = Vector2(1.f, 1.f)} world.appearance}
let getAppearance id world = Map.tryFind id world.appearance
//end components


//systems

//updates entities with position and velocity
let runMovement dt world =
    let advance (pos:Position) vel = ( pos + (dt * vel) : Position)
    let updatePos id pos =  
        let position = getPosition id world
        let velocity = getVelocity id world

        if Set.contains id world.entities && Option.isSome position && Option.isSome velocity then
            advance (Option.get position) (Option.get velocity)
        else
            pos

    let nextPositions = Map.map updatePos world.position
                                                     
    {world with position = nextPositions}

//draw entities with position and appearance
let runAppearance (sb:SpriteBatch) world =
    let draw id =
        let position = getPosition id world
        let appearance = getAppearance id world
        if Set.contains id world.entities && Option.isSome position && Option.isSome appearance then
            let position' = Option.get position
            let appearance' = Option.get appearance
            sb.Draw(appearance'.texture, position', Color.White)
        ()
    Map.iter (fun id _ -> draw id) world.appearance

let createPaddle id pos world =
    createEntity id world |>
    addPosition id defaultPosition |>
    addVelocity id defaultVelocity

let createBall id pos world =
    createEntity id world |>
    addPosition id defaultPosition |>
    addVelocity id (Vector2(0.f,0.f))