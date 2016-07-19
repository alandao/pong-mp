module HelperFunctions

open System.Collections.Generic

module DictionaryX =
    let TryFind x (dict:Dictionary<'a,'b>) = if dict.ContainsKey(x) then Some(dict.[x]) else None 
