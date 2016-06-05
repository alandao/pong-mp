module HelperFunctions

open System.Collections.Generic

let tryFind id (dict:Dictionary<'a,'b>) = if dict.ContainsKey(id) then Some(dict.[id]) else None