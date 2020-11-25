namespace MeMySelf
{
    using System.Collections.Generic;
    using System;

//Cubo Semantico Un mapa que define el output type para las operaciones y two input types  char + char = (error), int + int = int, float + float = float
//no puedes hacer operaciones con los letreros que son los strings.
    public class CuboSemantico
    {
        private bool DebugMode;

        //Lo que quiero es un estilo de mapa en el que pueda poder obtener un output y asi sea mas facil entenderlo
        //yo y sea mas visual
        //El orden de la tabla esta basado en el orden del symbol table en enum mytype
        private Dictionary<Op, MyType[][]> cuboSemantico = new Dictionary<Op, MyType[][]>()
        {
            { 
                Op.ADD, new MyType[][] //3*3 
                {
                    //          int         float           char
                    // int      int         float           error
                    // float    float       float           error
                    // char     error       error           error

                    //Aqui defino que int + float = float
                    new MyType[] {MyType.intType, MyType.floatType, MyType.error},
                    new MyType[] {MyType.floatType, MyType.floatType, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                }
            },
            {
                Op.SUB, new MyType[][]
                {
                    //          int         float           char
                    // int      int         float           error
                    // float    float       float           error
                    // char     error       error           error
                    new MyType[] {MyType.intType, MyType.floatType, MyType.error},
                    new MyType[] {MyType.floatType, MyType.floatType, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                }
            },
            {
                Op.MUL, new MyType[][]
                {
                    //          int         float           char
                    // int      int         float           error
                    // float    float       float           error
                    // char     error       error           error
                    new MyType[] {MyType.intType, MyType.floatType, MyType.error},
                    new MyType[] {MyType.floatType, MyType.floatType, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                }
            },
            {
                Op.DIV, new MyType[][]
                {
                    //          int         float           char
                    // int      float       float           error
                    // float    float       float           error
                    // char     error       error           error
                    new MyType[] {MyType.intType, MyType.floatType, MyType.error},
                    new MyType[] {MyType.floatType, MyType.floatType, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                }
            },
            {
                Op.SET, new MyType[][]
                {
                    // Left = Right
                    // var float a; 
                    // a = 5 + 2;
                    //Aqui le asigno a un int ej. pero termina siendo un float porque a es un float 

                    // var int a;
                    // a = .2
                    // Error

                    //          int         float           char
                    // int      int         error           error
                    // float    float       float           error
                    // char     error       error           char
                    new MyType[] {MyType.intType, MyType.error, MyType.error},
                    new MyType[] {MyType.floatType, MyType.floatType, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.charType},
                }
            },
            {
                Op.LT, new MyType[][]
                {
                    //Less than <
                    //Solo regresaria 1 o 0 si es verdad porque no existen los booleanos
                    //          int         float           char
                    // int      int         int             error
                    // float    int         int             error
                    // char     error       error           error
                    new MyType[] {MyType.intType, MyType.intType, MyType.error},
                    new MyType[] {MyType.intType, MyType.intType, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                }
            },
            {
                Op.LTE, new MyType[][]
                {
                    //Less than equal to <=
                    //          int         float           char
                    // int      int         int             error
                    // float    int         int             error
                    // char     error       error           error
                    new MyType[] {MyType.intType, MyType.intType, MyType.error},
                    new MyType[] {MyType.intType, MyType.intType, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                }
            },
            {
                Op.GT, new MyType[][]
                {
                    // Greater than >
                    //          int         float           char
                    // int      int         int             error
                    // float    int         int             error
                    // char     error       error           error
                    new MyType[] {MyType.intType, MyType.intType, MyType.error},
                    new MyType[] {MyType.intType, MyType.intType, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                }
            },
            {
                Op.GTE, new MyType[][]
                {
                    //Greater than equal >=
                    //          int         float           char
                    // int      int         int             error
                    // float    int         int             error
                    // char     error       error           error
                    new MyType[] {MyType.intType, MyType.intType, MyType.error},
                    new MyType[] {MyType.intType, MyType.intType, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                }
            },
            {
                Op.EQU, new MyType[][]
                {
                    //= Comparo las cosas si es verdadero regreso int o sea un 1
                    //          int         float           char
                    // int      int         error           error
                    // float    error       int             error
                    // char     error       error           int
                    new MyType[] {MyType.intType, MyType.error, MyType.error},
                    new MyType[] {MyType.error, MyType.intType, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.intType},
                }
            },
            {
                Op.NEQ, new MyType[][]
                {
                    // != solo puedo comparar cosas del mismo tipo
                    //Comparo las cosas si es verdadero regreso int o sea un 1
                    //          int         float           char
                    // int      int         error           error
                    // float    error       int             error
                    // char     error       error           int
                    new MyType[] {MyType.intType, MyType.error, MyType.error},
                    new MyType[] {MyType.error, MyType.intType, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.intType},
                }
            },
            {
                Op.AND, new MyType[][]
                {
                    //Solo funciona en ints porque es la unica manera que tengo como booleano
                    //          int         float           char
                    // int      int         error           error
                    // float    error       error           error
                    // char     error       error           error
                    new MyType[] {MyType.intType, MyType.error, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                }
            },
            {
                Op.OR, new MyType[][]
                {
                    //operaciones booleanas tambien solo se puede con ints
                    //          int         float           char
                    // int      int         error           error
                    // float    error       error           error
                    // char     error       error           error
                    new MyType[] {MyType.intType, MyType.error, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                    new MyType[] {MyType.error, MyType.error, MyType.error},
                }
            },
        };

        //Debugging
        public CuboSemantico(bool debug = false)
        {
            DebugMode = debug;
        }

        //Proceso de Cubo Semantico 
        public MyType GetType(Op oper, MyType oper_izq, MyType oper_der)
        {
            //si no contiene esta key o si intentan usar una operacion que no tenemos te debe regresar un error
            //ejemplo si intentan ++ o algo asi te va decir que cubo semantico no sabe de eso
            if (!cuboSemantico.ContainsKey(oper))
            {
                if (DebugMode) Console.Out.WriteLine($"CuboSemantico no entiende {oper}");
                return MyType.error;
            }

            //Ahora checo 
            //cubo ahora tiene el arreglo de la operacion que elegiste ej. OP.ADD y le entrega la matriz de 3*3
            MyType[][] cubo = cuboSemantico[oper];

            //solo estoy checando que no me vaya del matriz 
            if ((int)oper_izq >= cubo.Length)
            {
                //imprime los detalles del error si esoty usando el debugger
                if (DebugMode) Console.Out.WriteLine($"cubo.Length={cubo.Length}, oper_izq={(int)oper_izq}");
                if (DebugMode) Console.Out.WriteLine($"CuboSemantico no entiende {oper}, {oper_izq}");
                return MyType.error;
            }

            // cubo parte es una de las filas de la matriz
            MyType[] cuboParte = cubo[(int)oper_izq];
            if ((int)oper_der >= cuboParte.Length)
            {
                if (DebugMode) Console.Out.WriteLine($"CuboSemantico no entiende {oper}, {oper_izq}, {oper_der}");
                return MyType.error;
            }

            if (DebugMode) Console.Out.WriteLine($"CuboSemantico success {oper}, {oper_izq}, {oper_der}");
            return cuboParte[(int)oper_der];
        }
    }
}