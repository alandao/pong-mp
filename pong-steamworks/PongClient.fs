module PongClient

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open PongEntity

type PongClient () as x =
    inherit Game()

    do x.Content.RootDirectory <- ""
    let graphics = new GraphicsDeviceManager(x)
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch>

    let CreateEntity' = CreateEntity x.Content
    let DrawEntity (sb:SpriteBatch) (entity:Entity) = 
        if entity.Texture.IsSome then
            do sb.Draw(entity.Texture.Value, entity.Position, Color.White)
        ()

    let WorldObjects = lazy ([("player.png", Player(Nothing), Vector2(10.f,28.f), Vector2(32.f,32.f), false);
                              ("obstacle.png", Obstacle, Vector2(10.f,60.f), Vector2(32.f,32.f), true);
                              ("", Obstacle, Vector2(42.f,60.f), Vector2(32.f,32.f), true);]
                             |> List.map CreateEntity')

    override x.Initialize() =
        do spriteBatch <- new SpriteBatch(x.GraphicsDevice)
        base.Initialize()

    override x.LoadContent() = 
        do WorldObjects.Force () |> ignore

    override x.Update (gameTime) =
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) then
            x.Exit();

    override x.Draw (gameTime) =
        x.GraphicsDevice.Clear Color.CornflowerBlue
        let DrawEntity' = DrawEntity spriteBatch
        do spriteBatch.Begin ()
        WorldObjects.Value |> List.iter DrawEntity'
        do spriteBatch.End ()

    override x.UnloadContent() =
        ()