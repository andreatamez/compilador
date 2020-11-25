program Functions;

var int: i, j, k;

int module f1(int a, int b)
var int: aaa, bbb;
{
    write(a, "---", b);
    aaa = a - 1;
    bbb = b + 1;
    if (a > 5)
    then
    {
        return (1 - a);
    }
    else
    {
        return (f1(a + 1, b * 2));
    }
    write("\r\n");
    write(a, " ", b, " ", aaa, " ", bbb);
    write("\r\n");
}

int module plusP(int q, int p)
{
    if (p == 0)
    then
    {
        return(q);
    }
    else
    {
        return(plusP(q, p - 1) + 1);
    }
}

main()
{
    write("hello\r\n");
    write("\r\n");
    write(f1(1, plusP(2, 3)),"\r\n", plusP(1, f1(2, 3)));
    write(f1(1, 2));
    write(f1(9, 9));
}