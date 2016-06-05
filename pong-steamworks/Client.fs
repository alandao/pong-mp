module Client

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

open SharedServerClient

//  COMPONENTS
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


let addAppearance id textureName (contentManager:ContentManager) (world:World)  =
    {world with appearance = Map.add id {texture = contentManager.Load<Texture2D> textureName; 
                                         size = Vector2(1.f, 1.f)} world.appearance}

//  SYSTEMS

//draw entities with position and appearance
let runAppearance (sb:SpriteBatch) world =
    let draw id =
        let position = Map.tryFind id world.position
        let appearance = Map.tryFind id world.appearance
        if Set.contains id world.entities && Option.isSome position && Option.isSome appearance then
            let position' = Option.get position
            let appearance' = Option.get appearance
            sb.Draw(appearance'.texture, position', Color.White)
        ()
    Map.iter (fun id _ -> draw id) world.appearance


//  OTHER

let (getClientInputs : PlayerInput list) =
    let mutable inputs = List.empty
    if (Keyboard.GetState().IsKeyDown(Keys.W)) then
        inputs <- PaddleUp true :: inputs
    if (Keyboard.GetState().IsKeyDown(Keys.S)) then
        inputs <- PaddleDown true :: inputs
    inputs