
using System.Collections.Generic;

using System.Linq;

public static class issue29 {
    
    public static tuple meshgrid2(params object [] arrs) {
        arrs = tuple(reversed(arrs));
        var lens = map(len, arrs);
        var dim = arrs.Count;
        var sz = 1;
        foreach (var s in lens) {
            sz *= s;
        }
        var ans = new List<object>();
        foreach (var (i, arr) in arrs.Select((_p_1,_p_2) => Tuple.Create(_p_2, _p_1))) {
            var slc = new List<int> {
                1
            } * dim;
            slc[i] = lens[i];
            var arr2 = asarray(arr).reshape(slc);
            foreach (var (j, sz) in lens.Select((_p_3,_p_4) => Tuple.Create(_p_4, _p_3))) {
                if (j != i) {
                    arr2 = arr2.repeat(sz, axis: j);
                }
            }
            ans.append(arr2);
        }
        return tuple(ans);
    }
}
