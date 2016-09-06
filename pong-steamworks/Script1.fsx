for i in 0..128 do
    printfn "%i\n" i

let testDict = new System.Collections.Generic.Dictionary<int, string>()
testDict.Add(1, "hi")
testDict.[1]
testDict.[1] <- "bye"
testDict.[1]