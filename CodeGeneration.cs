// Andrea Tamez A01176494
// clases de CoCo
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace MeMySelf
{
    public enum Op
    { 
        // opcodes
        ADD, SUB, MUL, DIV, // +, -, *, /
        PAREN, EMPTY,       // ), 
        AND, OR,            // &, |

        SET,                // =
        GT, GTE, LT, LTE,   // >, >=, <, <=
        EQU, NEQ,           // ==, !=

        READ, WRITE,        // "read", "write"

        GOTO, GOTOF, GOTOV,       // for, if, while, do

        ERA, GOSUB, PARAMETER, RETURN, LOADRETURN, ENDFUNC,

        VER, VARADDRESS, DEREF,
        
        /*ADD, SUB, MUL, DIV,  EQU, */ LSS, GTR, NEG,
        LOAD, LOADG, STO, STOG, CONST,
        CALL, RET, ENTER, LEAVE, JMP, FJMP, /*READ, WRITE*/
        
    }

    public class CodeGenerator
    {
        public CodeGenerator () { pc = 1; progStart = 1; }
        string[] opcode =
        {"ADD ", "SUB ", "MUL ", "DIV ", "EQU ", "LSS ", "GTR ", "NEG ",
        "LOAD ", "LOADG", "STO ", "STOG ", "CONST", "CALL ", "RET ", "ENTER",
        "LEAVE", "JMP ", "FJMP ", "READ ", "WRITE"};

        public int progStart; // address of first instruction of main program
        public int pc; // program counter
        byte[] code = new byte[3000];
        // for data interpret
        int[] globals = new int[100];
        int[] stack = new int[100];
        int top; // top of stack
        int bp; // base pointer

        private bool DebugMode;

        public FuncTable funcTable; //Esta es la lista de todas las funciones que existen en memyself code file
        public VarTable globalTable; // Estas son las listas de las variables globales que existen en memyself code file.
        public VarTable currentScope; // variables locales dependiendo de donde este ejecutando
        public VarTable currentScopeParams; 
        public VarTable constTable;
        public ILogger logger; 

        public List<Quadruple> quads = new List<Quadruple>();

        public CodeGenerator(ILogger logger, bool debug = false)
        {
            DebugMode = debug;
            funcTable = new FuncTable(logger, 10000);
            constTable = new VarTable(logger, 0);
            ResetCurrentScopeTable();
        }

        //----- code generation methods -----
        public void Put(int x) { code[pc++] = (byte)x; }
        public void Emit (Op op) { Put((int)op); }
        public void Emit (Op op, int val) { Emit(op); Put(val>>8); Put(val); }
        public void Patch (int adr, int val) {
            code[adr] = (byte)(val>>8); code[adr+1] = (byte)val;
        }

        //Agrega un nuevo cuadruplo y lo agrega a la lista
        //cuando hacemos un cuadruplo tambien te dice que numero es
        public int AddQuad(Op op, int oper_izq, int oper_der, int target)
        {
            Quadruple q = new Quadruple(op, oper_izq, oper_der, target);
            if (DebugMode) Console.Out.WriteLine($"[CG]:: {q}");
            quads.Add(q);
            return Cont() - 1;
        }

        public int Cont()
        {
            return quads.Count;
        }

        public void Fill(int quadCont, int jumpLoc)
        {
            Quadruple q = quads[quadCont];
            if (DebugMode) Console.Out.WriteLine($"[CG]:: q={q}");
            if (!(q.oper == Op.GOTOF || q.oper == Op.GOTO || q.oper == Op.GOTOV))
            {
                logger.SemErr($"CodeGen err - bad FILL. qc={quadCont}, jL={jumpLoc}");
                return;
            }

            q.target = jumpLoc;
            if (DebugMode) Console.Out.WriteLine($"[CG.Fill]:: q={q}");
        }

        public int NextRegister(MyType type)
        {
            string rName = $"__T{currentScope.RowCount()}";
            currentScope.AddData(new VarRow(rName, type, -1));

            VarRow reg = currentScope.Find(rName);
            return reg.address;
        }

        public int NextArrayRegister(MyType type, params int[] dims)
        {
            string rName = $"__T{currentScope.RowCount()}";
            currentScope.AddData(new VarArray(rName, type, -1, dims));

            VarRow reg = currentScope.Find(rName);
            return reg.address;
        }

        public void AddFunction(FuncRow func)
        {
            funcTable.AddData(func);
        }

        public void UpdateFuncTableAddressing(VarTable table)
        {
            funcTable.UpdateAddressNext(table);
        }

        public FuncRow GetFunction(string name)
        {
            return funcTable.Find(name);
        }
        
        public VarTable GetEmptyTable(int addressStart = 0)
        {
            return new VarTable(logger, addressStart);
        }

        public void SetGlobalTable(VarTable table)
        {
            this.globalTable = table;
        }

        public void SetCurrentScopeParamsTable(VarTable table)
        {
            this.currentScopeParams = table;
        }

        public void SetCurrentScopeTable(VarTable table)
        {
            if (DebugMode) Console.Out.WriteLine($"[CG]::Updating current scope table in codegen");
            this.currentScope = table;
        }

        public void ResetCurrentScopeTable()
        {
            this.currentScope = GetEmptyTable();
            this.currentScopeParams = GetEmptyTable();
        }

        public int AddConstanteInt(string val)
        {
            int unused;
            if (!int.TryParse(val, out unused))
            {
                logger.SemErr($"Invalid int: {val}");
                return -1000;
            }

            if (!constTable.CheckName(val))
            {
                constTable.AddData(new VarRow(val, MyType.intType, 0));
            }

            return constTable.Find(val).address;
        }

        public int AddConstanteFloat(string val)
        {
            float unused;
            if (!float.TryParse(val, out unused))
            {
                logger.SemErr($"Invalid float: {val}");
                return -1000;
            }

            if (!constTable.CheckName(val))
            {
                constTable.AddData(new VarRow(val, MyType.floatType, 0));
            }
            
            return constTable.Find(val).address;
        }

        public int AddConstanteChar(string val)
        {
            if (!constTable.CheckName(val))
            {
                constTable.AddData(new VarRow(val, MyType.charType, 0));
            }
            
            return constTable.Find(val).address;
        }

        public int AddConstanteLetrero(string val)
        {
            if (!constTable.CheckName(val))
            {
                string trueSize = val.Replace(@"\r", "\r").Replace(@"\n", "\n");
                constTable.AddData(new VarArray(val, MyType.charType, 0, 0, trueSize.Length - 3));
            }

            return constTable.Find(val).address;
        }
        
        public FuncRow GetFuncFromAddress(int address)
        {
            FuncRow row = null;
            funcTable.TryFindByAddress(address, out row);
            if (DebugMode) Console.Out.WriteLine($"[CG]::{address}, {row}<<");
            return row;
        }

        public VarRow GetVar(string id) /*primero ve en la scope y luego en la global scope te permite tener una variable si estan en diferentes */
        {
            VarRow row;
            if (currentScope.CheckName(id))
            {
                row = currentScope.Find(id);
            }
            else if (currentScopeParams.CheckName(id))
            {
                row = currentScopeParams.Find(id);
            }
            else
            {
                row = globalTable.Find(id);
            }

            return row;
        }

        public VarRow GetVarFromAddress(int address)
        {
            VarRow row;
            if (currentScope.TryFindByAddress(address, out row))
            {
                return row;
            } else if (currentScopeParams.TryFindByAddress(address, out row))
            {
                return row;
            }
            else if (globalTable.TryFindByAddress(address, out row))
            {
                return row;
            }
            else if (constTable.TryFindByAddress(address, out row))             
            {
                return row;
            }

            return null;
        }

        public VarArray GetArray(string id)
        {
            VarRow row = GetVar(id);
            if (!row.IsArray)
            {
                logger.SemErr($"{id} no es Array");
            }

            return row as VarArray;
        }

        public void SaveCodeToObjectFile(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine(quads.Count);
                foreach (Quadruple q in quads)
                {
                    sw.WriteLine(q.Serialize());
                }

                sw.WriteLine(funcTable.Serialize());
                sw.WriteLine(globalTable.Serialize());
                sw.WriteLine(currentScope.Serialize());
                sw.WriteLine(constTable.Serialize());
            }
        }

        public static CodeGenerator ReadCodeFromObjectFile(ILogger logger, string fileName)
        {
            using (StreamReader sr = new StreamReader(fileName))
            {
                int quadCount = int.Parse(sr.ReadLine());
                List<Quadruple> list = new List<Quadruple>();
                for (int i = 0; i < quadCount; ++i)
                {
                    list.Add(Quadruple.Deserialize(sr.ReadLine()));
                }

                FuncTable funcTable = FuncTable.Deserialize(logger, sr.ReadLine());
                VarTable globalTable = VarTable.Deserialize(logger, sr.ReadLine());
                VarTable currentScope = VarTable.Deserialize(logger, sr.ReadLine());
                //funcTable.GetDataList()[0].DeserializeUpdateLocalVars(currentScope);
                VarTable constTable = VarTable.Deserialize(logger, sr.ReadLine());

                CodeGenerator codegen = new CodeGenerator(logger);
                codegen.quads = list;
                codegen.funcTable = funcTable;
                codegen.globalTable = globalTable;
                codegen.currentScope = currentScope;
                codegen.constTable = constTable;

                return codegen;
            }
        }

        //funciones de coco
        public void Decode()
        {
            int maxPc = pc; pc = 1;
            while (pc < maxPc)
            {
                Op code = (Op)Next();
                Console.Write("{0,3}: {1} ", pc-1, opcode[(int)code]);
                switch(code)
                {
                    case Op.LOAD: case Op.LOADG: case Op.CONST: case Op.STO: case Op.STOG:
                    case Op.CALL: case Op.ENTER: case Op.JMP: case Op.FJMP:
                    Console.WriteLine(Next2()); break;
                    case Op.ADD: case Op.SUB: case Op.MUL: case Op.DIV: case Op.NEG:
                    case Op.EQU: case Op.LSS: case Op.GTR: case Op.RET: case Op.LEAVE:
                    case Op.READ: case Op.WRITE:
                    Console.WriteLine(); break;
                }
            }
        }

        //----- interpreter methods -----
        int Next ()
        {
            return code[pc++];
        }

        int Next2 ()
        {
            int x, y;
            x = (sbyte)code[pc++]; y = code[pc++];
            return (x << 8) + y;
        }
        
        int Int (bool b)
        {
            if (b) return 1; else return 0;
        }

        void Push (int val)
        {
            stack[top++] = val;
        }

        int Pop()
        {
            return stack[--top];
        }

        int ReadInt(Stream s)
        {
            int ch, sign, n = 0;
            do {ch = s.ReadByte();} while (!(ch >= '0' && ch <= '9' || ch == '-'));
            if (ch == '-') {sign = -1; ch = s.ReadByte();} else sign = 1;
            while (ch >= '0' && ch <= '9') {
                n = 10 * n + (ch - '0');
                ch = s.ReadByte();
            }

            return n * sign;
        }

        public void Interpret (Stream inputStream)
        {
            int val;
            try {
                Console.WriteLine();
                pc = progStart; stack[0] = 0; top = 1; bp = 0;
                for (;;) {
                    switch ((Op)Next()) {
                        case Op.CONST: Push(Next2()); break;
                        case Op.LOAD: Push(stack[bp+Next2()]); break;
                        case Op.LOADG: Push(globals[Next2()]); break;
                        case Op.STO: stack[bp+Next2()] = Pop(); break;
                        case Op.STOG: globals[Next2()] = Pop(); break;
                        case Op.ADD: Push(Pop()+Pop()); break;
                        case Op.SUB: Push(-Pop()+Pop()); break;
                        case Op.DIV: val = Pop(); Push(Pop()/val); break;
                        case Op.MUL: Push(Pop()*Pop()); break;
                        case Op.NEG: Push(-Pop()); break;
                        case Op.EQU: Push(Int(Pop()==Pop())); break;
                        case Op.LSS: Push(Int(Pop()>Pop())); break;
                        case Op.GTR: Push(Int(Pop()<Pop())); break;
                        case Op.JMP: pc = Next2(); break;
                        case Op.FJMP: val = Next2(); if (Pop()==0) pc = val; break;
                        case Op.READ: val = ReadInt(inputStream); Push(val); break;
                        case Op.WRITE: Console.WriteLine(Pop()); break;
                        case Op.CALL: Push(pc+2); pc = Next2(); break;
                        case Op.RET: pc = Pop(); if (pc == 0) return; break;
                        case Op.ENTER: Push(bp); bp = top; top = top + Next2(); break;
                        case Op.LEAVE: top = bp; bp = Pop(); break;
                        default: throw new Exception("illegal opcode");
                    }
                }
            } catch (IOException) {
                Console.WriteLine("--- Error accessing file ???");
                System.Environment.Exit(0);
            }
        }
    }
}