program Functions;

var int: i;

int module plusP(int p)
{
    write(p, " ");
    if (p > 0)
    then
    {
        return(plusP(p - 1) + 1);
    }
    else
    {
        write("\r\n");
        return(0);
    }
}

main()
{
    i = plusP(100);
   
    write(">>", i + plusP(50), "\r\n");
    
}