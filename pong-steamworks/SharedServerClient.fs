module SharedServerClient

open Microsoft.Xna.Framework
open System.Collections.Generic

open Components







type PlayerInput = 
    | PaddleUp of bool
    | PaddleDown of bool

type ClientToServerMsg = 
    | PaddleAt of x : float * y : float


//  Server Message types
let msg_snapshot = 0

