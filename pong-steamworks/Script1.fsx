type RecordTest = 
    {
        x : System.Collections.Generic.Dictionary<int, int>
    }

let dummyTest() = { x = new System.Collections.Generic.Dictionary<int,int>() }

let a = dummyTest()
let b = dummyTest()

a.x.Add(1, -1)
b.x.Add(1, -2)

let test1 = a.x.[1]
let test2 = b.x.[1]
