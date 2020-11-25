program Andrea;

var int : i[5], j[-1 .. 3][6], kk, pj[2][3][4];
    float : fff, fDim[99][99], qPlum[-14 .. 0];

int module f1(int a)
{
  return(a + a * a);
}

void module hola (int b)
var int : c;
    char : letrero[188];
{
  write(b+b);
}

main()
{
  from kk = 0 to 9 do
  { 
    if (kk * 2 < 7) then
    { 
      write(5);
      write("hello");
    }
    
    %%read(j);
    %%hola(j+i);
  }

  %%j = f1(i);
  %%write(j);
}

