// Andrea Tamez A01176494
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MeMySelf
{
    public class VM
    {
        /*
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
        */

        public int[] code;
        public byte[] stack;
        public int pc;
        public CodeGenerator codegen;
        public bool DebugMode = false;
        public Stack<int> pcCallStack = new Stack<int>();
        public Stack<FuncRow> funcCallStack = new Stack<FuncRow>();
        public FuncRow globalFunc;

        public const int STACK_SIZE = 1000000;
        public int stackPointer;

        public VM(CodeGenerator codegen)
        {
            pc = 0;
            globalFunc = codegen.funcTable.GetDataList()[0];
            funcCallStack.Push(globalFunc);
            code = new int[codegen.quads.Count*4];
            stack = new byte[STACK_SIZE];
            stackPointer = codegen.funcTable.addressNext;
            this.codegen = codegen;

            for (int i = 0; i < code.Length; i += 4)
            {
                Quadruple q = codegen.quads[i/4];
                code[i] = (int)q.oper;
                code[i+1] = q.oper_izq;
                code[i+2] = q.oper_der;
                code[i+3] = q.target;
            }

            foreach (VarRow row in codegen.constTable.GetDataList())
            {
                byte[] bytes = null;
                if (row is VarArray)
                {
                    switch (row.type)
                    {
                        case MyType.charType:
                            string val = row.name.Replace(@"\r", "\r").Replace(@"\n", "\n");
                            val = val.Substring(1, val.Length - 2);
                            bytes = Encoding.ASCII.GetBytes(val);
                            break;
                        default:
                            throw new Exception($"not supported const array type: {row.type}");
                    }
                }
                else
                {
                    switch (row.type)
                    {
                        case MyType.intType:
                            int t = int.Parse(row.name);
                            bytes = BitConverter.GetBytes(t);
                            break;
                        case MyType.charType:
                            char c = row.name[0];
                            bytes = BitConverter.GetBytes(c);
                            break;
                        case MyType.floatType:
                            float f = float.Parse(row.name);
                            bytes = BitConverter.GetBytes(f);
                            break;
                        default:
                            throw new Exception($"not supported type: {row.type}");
                    }
                }

                Array.Copy(bytes, 0, stack, row.address, bytes.Length);
            }
        }

        public int Next()
        {
            return code[pc++];
        }

        public VarRow GetRow(int address)
        {
            if (address == -1)
            {
                return null;
            }
            
            return codegen.GetVarFromAddress(address);
        }

        public MyType GetType(int address)
        {
            if (address == -1)
            {
                return MyType.undef;
            }

            VarRow row = codegen.GetVarFromAddress(address);
            if (DebugMode) Console.Out.WriteLine($"[VM]::GetVarFromAddress::{address}, {row?.name}, {row?.type}");
            if (row != null)
            {
                return row.type;
            }

            return MyType.undef;
        }

        public void Interpret(TextReader inputStream)
        {
            while (true)
            {
                if (pc == code.Length)
                {
                    return;
                }

                Op op = (Op)Next();
                int oper_izq = Next();
                int oper_der = Next();
                int target = Next();

                VarRow izq_row = GetRow(oper_izq);
                MyType izq_type = GetType(oper_izq);
                MyType der_type = GetType(oper_der);
                MyType retType = GetType(target);
                object izq_val = ReadAddressAs(oper_izq, izq_type, retType);
                object der_val = ReadAddressAs(oper_der, der_type, retType);
                FuncRow func;
                if (DebugMode) Console.Out.WriteLine($"[VM]::Op={op}");
                switch (op)
                {        
/*


        ERA, GOSUB, PARAMETER, RETURN, LOADRETURN, ENDFUNC,

        VER, VARADDRESS, DEREF,
        */
                    case Op.ADD:
                        switch (retType)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val + (int)der_val);
                                break;
                            case MyType.floatType:
                                StoreAddress(target, retType, (float)izq_val + (float)der_val);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.SUB:
                        switch (retType)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val - (int)der_val);
                                break;
                            case MyType.floatType:
                                StoreAddress(target, retType, (float)izq_val - (float)der_val);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.MUL:
                        switch (retType)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val * (int)der_val);
                                break;
                            case MyType.floatType:
                                StoreAddress(target, retType, (float)izq_val * (float)der_val);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.DIV:
                        switch (retType)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val / (int)der_val);
                                break;
                            case MyType.floatType:
                                StoreAddress(target, retType, (float)izq_val / (float)der_val);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.AND:
                        switch (retType)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val != 0 && (int)der_val != 0);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.OR:
                        switch (retType)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val != 0 || (int)der_val != 0);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.GT:
                        switch (izq_type)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val > (int)der_val);
                                break;
                            case MyType.floatType:
                                StoreAddress(target, retType, (float)izq_val > (float)der_val);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.GTE:
                        switch (izq_type)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val >= (int)der_val);
                                break;
                            case MyType.floatType:
                                StoreAddress(target, retType, (float)izq_val >= (float)der_val);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.LT:
                        switch (izq_type)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val < (int)der_val);
                                break;
                            case MyType.floatType:
                                StoreAddress(target, retType, (float)izq_val < (float)der_val);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.LTE:
                        switch (izq_type)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val <= (int)der_val);
                                break;
                            case MyType.floatType:
                                StoreAddress(target, retType, (float)izq_val <= (float)der_val);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.EQU:
                        switch (izq_type)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val == (int)der_val);
                                break;
                            case MyType.floatType:
                                StoreAddress(target, retType, (float)izq_val == (float)der_val);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.NEQ:
                        switch (izq_type)
                        {
                            case MyType.intType:
                                StoreAddress(target, retType, (int)izq_val != (int)der_val);
                                break;
                            case MyType.floatType:
                                StoreAddress(target, retType, (float)izq_val != (float)der_val);
                                break;
                            default:
                                throw new Exception($"Invalid type: {retType}");
                        }
                        break;
                    case Op.SET:
                        StoreAddress(target, retType, izq_val);
                        break;
                    case Op.READ:
                        StoreAddress(oper_izq, izq_type, Read(inputStream, izq_type));
                        break;
                    case Op.WRITE:
                        if (izq_row.IsArray)
                        {
                            string res = Encoding.ASCII.GetString(stack, izq_row.address, izq_row.totalSize);
                            Console.Write(res);
                        }
                        else
                        {
                            Console.Write(izq_val);
                        }
                        break;
                    case Op.GOTO:
                        pc = 4 * target;
                        break;
                    case Op.GOTOF:
                        if (((int)izq_val) == 0)
                        {
                            pc = 4 * target;
                        }
                        break;
                    case Op.GOTOV:
                        if (((int)izq_val) != 0)
                        {
                            pc = 4 * target;
                        }
                        break;
                    case Op.ERA:
                        func = codegen.GetFuncFromAddress(oper_izq);
                        // create temp loc on stack with new mems
                        stackPointer += func.parameters.TotalSize();
                        break;
                    case Op.PARAMETER: // exp, row.address, counter
                        func = codegen.GetFuncFromAddress(oper_der);
                        izq_val = ReadAddressAs(oper_izq, izq_type, izq_type);
                        VarRow paramRow = func.parameters.GetDataList()[target];
                        StoreAddress(stackPointer - func.parameters.TotalSize() + paramRow.address - func.parameters.addressStart, paramRow.type, izq_val);
                        // set address into temp loc on stack
                        break;
                    case Op.GOSUB:
                        // create temp loc on stack with new mems
                        // copy existing datas over to new mems
                        // copy ERA data over into func mems
                        func = codegen.GetFuncFromAddress(target);
                        pcCallStack.Push(pc);
                        funcCallStack.Push(func);
                        pc = target*4;
                        int privateStackPointer = stackPointer - func.parameters.TotalSize();
                        MoveFuncVarsToStack(func);
                        CopyFromStackSection(privateStackPointer, func.parameters);
                        if (DebugMode) Console.Out.WriteLine($"[VW]::Setting CurrentScopeParams to params of {func.name}. {func.parameters.addressStart}");
                        codegen.SetCurrentScopeParamsTable(func.parameters);
                        if (DebugMode) Console.Out.WriteLine($"[VW]::Setting CurrentScope to localVars of {func.name}, {func.localVars.addressStart}");
                        codegen.SetCurrentScopeTable(func.localVars);
                        break;
                    case Op.ENDFUNC:
                        // copy old datas over to func mems
                        // free up ERA data
                        funcCallStack.Pop();
                        func = funcCallStack.Peek();
                        codegen.SetCurrentScopeParamsTable(func.parameters);
                        codegen.SetCurrentScopeTable(func.localVars);

                        MoveFuncVarsToFunc(func);
                        pc = pcCallStack.Pop();
                        break;
                    case Op.RETURN:
                        StoreAddress(oper_der, der_type, izq_val);
                        break;
                    case Op.LOADRETURN:
                        break;
                        /*ERA, GOSUB, PARAMETER, RETURN, LOADRETURN, ENDFUNC,

        VER, VARADDRESS, DEREF,*/
                    default:
                        throw new Exception($"Unsupported OP:{op}");

                }
            }
        }

        public void MoveFuncVarsToFunc(FuncRow func)
        {
            if (func != globalFunc)
            {
                CopyFromStackSection(stackPointer - func.totalSize, func.parameters);
                CopyFromStackSection(stackPointer - func.totalSize + func.parameters.TotalSize(), func.localVars);

                stackPointer -= func.totalSize;
            }
        }

        // Copies the func variable data to a reserved location on the stack
        public void MoveFuncVarsToStack(FuncRow func)
        {
            if (func != globalFunc)
            {
                CopyToStackSection(stackPointer, func.parameters);
                CopyToStackSection(stackPointer + func.parameters.TotalSize(), func.localVars);

                stackPointer += func.totalSize;
            }
        }

        public void CopyFromStackSection(int locationFrom, VarTable table)
        {
            foreach (VarRow param in table.GetDataList())
            {
                object val;
                if (param.IsArray)
                {
                    StoreAddress(param.address, param.type, locationFrom + param.address - table.addressStart, true, param.totalSize);
                }
                else
                {
                    val = ReadAddressAs(locationFrom + (param.address - table.addressStart), param.type, param.type);
                    StoreAddress(param.address, param.type, val);
                }
            }
        }

        // Copies a specific VarTable to another location on the stack
        public void CopyToStackSection(int locationTo, VarTable table)
        {
            foreach (VarRow param in table.GetDataList())
            {
                object val;
                if (param.IsArray)
                {
                    StoreAddress(locationTo + param.address - table.addressStart, param.type, param.address, true, param.totalSize);
                }
                else
                {    
                    val = ReadAddressAs(param.address, param.type, param.type);
                    StoreAddress(locationTo + (param.address - table.addressStart), param.type, val);
                }
            }
        }

        public Queue<string> buffer = new Queue<string>();

        public object Read(TextReader sr, MyType type)
        {
            string next = null;
            if (buffer.Count == 0)
            {
                string[] parts = sr.ReadLine().Split(new string[] {"\t", " ", "\r\n"}, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    buffer.Enqueue(part);
                }
            }

            next = buffer.Dequeue();
            switch (type)
            {
                case MyType.intType:
                    return int.Parse(next);
                case MyType.floatType:
                    return float.Parse(next);
                case MyType.charType:
                    if (next.Length != 1)
                    {
                        throw new Exception($"error, {next} is not a character");
                    }
                    return next[0];
                default:
                    throw new Exception($"unsupported type {type}");
            }
        }

        public void StoreAddress(int address, MyType type, bool value)
        {
            if (DebugMode) Console.Out.WriteLine($"[VM]::StoreAddress::{address},{type},{value}");
            StoreAddress(address, type, value ? 1 : 0);
        }

        public void StoreAddress(int address, MyType type, object value, bool isArray = false, int size = -1)
        {
            if (DebugMode) Console.Out.WriteLine($"[VM]::StoreAddress::{address},{type},{value},{isArray},{size}");

            if (isArray)
            {
                Array.Copy(stack, (int)value, stack, address, size);
            }
            else
            {
                switch (type)
                {
                    case MyType.intType:
                        StoreInt(address, (int)value);
                        break;
                    case MyType.floatType:
                        StoreFloat(address, (float)value);
                        break;
                    case MyType.charType:
                        StoreChar(address, (char)value);
                        break;
                    default:
                        throw new Exception($"invalid type:{type}");
                }
            }
        }

        public void StoreInt(int address, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, stack, address, bytes.Length);
            if (DebugMode) Console.Out.WriteLine($"[VM]::StoreInt::{address}, {value}");
        }

        public void StoreFloat(int address, float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, stack, address, bytes.Length);
        }

        public void StoreChar(int address, char value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, stack, address, bytes.Length);
        }

        public object ReadAddressAs(int address, MyType trueType, MyType castType)
        {
            if (DebugMode) Console.Out.WriteLine($"[VM]::ReadAddressAS::{address}, {trueType}, {castType}");
            if (address == -1)
            {
                return -1;
            }

            object val;
            switch (trueType)
            {
                case MyType.intType:
                    val = ReadInt(address);
                    break;
                case MyType.floatType:
                    val = ReadFloat(address);
                    break;
                case MyType.charType:
                    val = ReadChar(address);
                    break;
                default:
                    val = ReadInt(address);
                    break;
                    //throw new Exception($"invalid type: {trueType}");
            }

            try
            {
                switch (castType)
                {
                    case MyType.intType:
                        return (int)val;
                    case MyType.floatType:
                        return (float)val;
                    case MyType.charType:
                        return (char)val;
                    default:
                        return val;
                }
            }
            catch
            {
                return null;
            }
        }

        public int ReadInt(int address)
        {
            return BitConverter.ToInt32(stack, address);
        }

        public float ReadFloat(int address)
        {
            return BitConverter.ToSingle(stack, address);
        }

        public char ReadChar(int address)
        {
            return BitConverter.ToChar(stack, address);
        }

/*
        public void OldInterpret (Stream inputStream)
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
        */
    }
}