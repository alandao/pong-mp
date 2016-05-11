module PongClient

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

type PongClient () as x =
    inherit Game()

    do x.Content.RootDirectory <- "Content"
    let graphics = new GraphicsDeviceManager(x)
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch>

    override x.Initialize() =
        do spriteBatch
        base.Initialize()

    override x.LoadContent() = 
        ()
    override x.Update (gameTime) =
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) then
            x.Exit();

        base.Update(gameTime)

    override x.Draw (gameTime) =
        x.GraphicsDevice.Clear Color.CornflowerBlue

        base.Draw(gameTime)

    override x.UnloadContent() =
        base.UnloadContent()