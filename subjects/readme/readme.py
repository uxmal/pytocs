class MyClass:
    # member function calling other function
    def calc_sum(self, x, y):
       return self.frobulate('+', x, y)

    # arithmetic and exceptions
    def frobulate(self, op, x, y):
        if op == '+':
            return x + y
        elif op == '-':
            return x - y
        else:
            raise ValueError("Unexpected argument %s" % op)

    # static method using for..in and enumerate, with tuple comprehension
    def walk_list(lst):
        for (i,strg) in lst.iterate():
            print "index: %d strg: %s\n" % (i, strg)
 
    # list comprehension, generating linQ output.
    def apply_map(mapfn, filterfn):
        return [mapfn(n) for n in lst if filterfn]
