module Client

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open System.Collections.Generic

open HelperFunctions
open SharedServerClient

//  COMPONENTS
type Appearance = { texture : Texture2D; size : Vector2 }


type World = {
    entities : HashSet<string>;

    position: Dictionary<string, Position>;
    velocity: Dictionary<string, Velocity>;
    appearance: Dictionary<string, Appearance>;
    }

let defaultWorld = {
    entities = HashSet<string>();
    position = Dictionary<string, Position>();
    velocity = Dictionary<string, Velocity>();
    appearance = Dictionary<string, Appearance>();
    }

let destroyEntity id world = 
    world.position.Remove(id) |> ignore
    world.velocity.Remove(id) |> ignore
    world.entities.Remove(id)

let addAppearance id textureName (contentManager:ContentManager) (world:World)  = 
    let appr =  {    
                    texture = contentManager.Load<Texture2D> textureName; 
                    size = Vector2(1.f, 1.f)
                }
    world.appearance.Add(id, appr)

//  SYSTEMS

//draw entities with position and appearance
let runAppearance (sb:SpriteBatch) world =
    for entry in world.appearance do
        let id = entry.Key
        let appearance = entry.Value
        let position = tryFind id world.position

        if Option.isSome position then
            sb.Draw(appearance.texture, Option.get position, Color.White)


//  OTHER

let (getClientInputs : PlayerInput list) =
    let mutable inputs = List.empty
    if (Keyboard.GetState().IsKeyDown(Keys.W)) then
        inputs <- PaddleUp true :: inputs
    if (Keyboard.GetState().IsKeyDown(Keys.S)) then
        inputs <- PaddleDown true :: inputs
    inputs