module PongEntity

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Content

type PhysicsType =
    | Static
    | Dynamic of Vector2

type PlayerState =
    | Nothing
    | Jumping

type EntityType =
    | Player of PlayerState
    | Obstacle

type Entity =
    {
        EntityType : EntityType;
        Position : Vector2;
        Size : Vector2;
        Texture : Texture2D option;
        PhysicsType : PhysicsType
    }

    member this.CurrentBounds
        with get () = Rectangle((int this.Position.X),(int this.Position.Y),(int this.Size.X),(int this.Size.Y))

    member this.DesiredBounds
        with get () = match this.PhysicsType with
                        | Dynamic(s) -> this.Position + s
                        | _-> this.Position

let CreateEntity (content:ContentManager) (textureName, entityType, position, size, isStatic) =
    let tex =   if not (System.String.IsNullOrEmpty textureName) then
                    Some(content.Load textureName)
                else
                    None
    let pt =    if isStatic then
                    Static
                else
                    Dynamic(Vector2(0.f,0.f))
    { EntityType = entityType; Position = position; Size = size; Texture = tex; PhysicsType = pt; }

