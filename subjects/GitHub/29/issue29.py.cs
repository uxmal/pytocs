
using System.Collections.Generic;

public static class issue29 {
    
    public static object meshgrid2(params object [] arrs) {
        arrs = tuple(reversed(arrs));
        var lens = map(len, arrs);
        var dim = arrs.Count;
        var sz = 1;
        foreach (var s in lens) {
            sz *= s;
        }
        var ans = new List<object>();
        foreach (var _tup_1 in arrs.Select((_p_1,_p_2) => Tuple.Create(_p_2, _p_1))) {
            var i = _tup_1.Item1;
            var arr = _tup_1.Item2;
            var slc = new List<int> {
                1
            } * dim;
            slc[i] = lens[i];
            var arr2 = asarray(arr).reshape(slc);
            foreach (var _tup_2 in lens.Select((_p_3,_p_4) => Tuple.Create(_p_4, _p_3))) {
                var j = _tup_2.Item1;
                sz = _tup_2.Item2;
                if (j != i) {
                    arr2 = arr2.repeat(sz, axis: j);
                }
            }
            ans.append(arr2);
        }
        return tuple(ans);
    }
}
