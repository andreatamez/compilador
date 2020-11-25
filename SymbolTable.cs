// Andrea Tamez A01176494
// clases de CoCo
namespace MeMySelf
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    //Obj es parte de symbol table que venia con coco.
    public class Obj { // object decribing a declared name
        public string name; // name of the object
        public MyType type; // type of the object (undef for procs)
        public Obj next; // to next object in same scope
        public MyObjKind kind; // var, proc, scope
        public int adr; // address in memory or start of proc
        public int level; // nesting level; 0=global, 1=local
        public Obj locals; // scopes: to locally declared objects
        public int nextAdr; // scopes: next free address in this scope
    }

    public enum MyType
    {
        intType = 0,
        floatType = 1,
        charType = 2,
        undef = 3,
        error = 4,
    }

    public static class MyTypeUtil
    {
        public static int GetSize(MyType type)
        {
            switch (type)
            {
                case MyType.charType:
                    return 1;
                case MyType.intType:
                case MyType.floatType:
                    return 4;
                default:
                    throw new Exception($"Tipo {type} no tiene tamano");
            }
        }

        public static bool IsValidReturnType(MyType type)
        {
            switch (type)
            {
                case MyType.intType:
                case MyType.charType:
                case MyType.floatType:
                    return true;
                default:
                    return false;
            }
        }
    }

    public enum MyObjKind
    {
        var = 0,
        proc = 1,
        scope = 2,
    }

    // Symbol Table era una clase que venia con coco pero no funciona de la misma 
    // manera que aprendimos en clase asi que en las versiones anteriores utilice esta pero me daba errores
    // no me explica que esta haciendo y mejor la escribi yo para que pueda entenderlo.
    public class SymbolTable {

        public int curLevel; // nesting level of current scope
        public Obj undefObj; // object node for erroneous symbols
        public Obj topScope; // topmost procedure scope
        Parser parser;
        // open a new scope and make it the current scope (topScope)
        public void OpenScope () {
            Obj scop = new Obj();
            scop.name = ""; scop.kind = MyObjKind.scope;
            scop.locals = null; scop.nextAdr = 0;
            scop.next = topScope; topScope = scop;
            curLevel++;
        }
        // close the current scope
        public void CloseScope () {
            topScope = topScope.next; curLevel--;
        }

        // create a new object node in the current scope
        public Obj NewObj (string name, MyObjKind kind, MyType type) {
            Obj p, last, obj = new Obj();
            obj.name = name; obj.kind = kind; obj.type = type;
            obj.level = curLevel;
            p = topScope.locals; last = null;
            while (p != null) {
            if (p.name == name) parser.SemErr("name declared twice");
            last = p; p = p.next;
            }
            if (last == null) topScope.locals = obj; else last.next = obj;
            if (kind == MyObjKind.var) obj.adr = topScope.nextAdr++;
            return obj;
        }

        // search the name in all open scopes and return its object node
        public Obj Find (string name) {
            Obj obj, scope;
            scope = topScope;
            while (scope != null) { // for all scopes
            obj = scope.locals;
            while (obj != null) { // for all objects in this scope
            if (obj.name == name) return obj;
            obj = obj.next;
            }
            scope = scope.next;
            }
            parser.SemErr(name + " is undeclared");
            return undefObj;
        }

        public SymbolTable (Parser parser) {
            this.parser = parser;
            topScope = null;
            curLevel = -1;
            undefObj = new Obj();
            undefObj.name = "undef"; undefObj.type = MyType.undef; undefObj.kind = MyObjKind.var;
            undefObj.adr = 0; undefObj.level = 0; undefObj.next = null;
        }
    } // end SymbolTable

    //Es la representacion de lo que es una variable
    //Una variable tiene un nombre y un tipo 
    //y tiene un address que dice donde existe y el size de cuanta memoria toma
    public class VarRow
    {
        public string name {get; private set;}
        public MyType type {get; private set;}
        public int address {get; protected set;} //aun no esta 100% funcional pero esta va ser para la virtual machine que sepa donde va estar guardada.
        public virtual int totalSize //que tanto espacio ocupa la variable para cuando la guardas en la machine, puede haber variables aue se overlappean y eso no funciona
        {
            get
            {
                return MyTypeUtil.GetSize(type);
            }
        }

        public virtual bool IsArray
        {
            get
            {
                return false;
            }
        }

        //Constructor
        public VarRow(string name, MyType type, int address)
        {
            this.name = name;
            this.type = type;
            this.address = address;
        }

        //Set the address despues del construction por si cambio la loc de la variable.
        public virtual void SetAddress(int address)
        {
            this.address = address;
        }

        public virtual string Serialize()
        {
            return $"VARROW,{name},{(int)type},{address}";
        }

        public static VarRow Deserialize(string input)
        {
            return VarRow.Deserialize(input.Split(','), 1);
        }

        public static VarRow Deserialize(string[] splits, int offset)
        {
            string name = splits[offset];
            MyType myType = (MyType)int.Parse(splits[offset+1]);
            int address = int.Parse(splits[offset+2]);
            //Console.Out.WriteLine($"[VarRow::Deserialize]-{splits[offset]}, {splits[offset+1]}, {splits[offset+2]}, STEP={3}");
            return new VarRow(name, myType, address);
        }
    }

    public class VarArray : VarRow
    {
        public int[] dimensionSizes;
        public int[] dimensionLow;

        public override int totalSize //que tanto espacio ocupa la variable para cuando la guardas en la machine, puede haber variables aue se overlappean y eso no funciona
        {
            get
            {
                int total = 1;
                foreach (int i in dimensionSizes)
                {
                    total *= i;
                }

                return MyTypeUtil.GetSize(type) * total;
            }
        }

        public override bool IsArray
        {
            get
            {
                return true;
            }
        }

        public int ArrayConstant {get; private set;}

        //Constructor
        public VarArray(string name, MyType type, int address, params int[] dimensions)
            : base(name, type, address)
        {
            if (dimensions.Length % 2 != 0)
            {
                throw new Exception($"Invalid tamano de dims: {dimensions.Length}");
            }

            this.dimensionSizes = new int[dimensions.Length/2];
            this.dimensionLow = new int[dimensions.Length/2];
            this.ArrayConstant = 0;

            for (int i = 0; i < dimensions.Length; i += 2)
            {
                if (dimensions[i] > dimensions[i+1])
                {
                    throw new Exception($"Invalid tamano de array: {dimensions[i]} > {dimensions[i+1]}");
                }

                dimensionSizes[i/2] = dimensions[i+1] - dimensions[i] + 1;
                dimensionLow[i/2] = dimensions[i];

                ArrayConstant *= dimensionSizes[i/2];
                ArrayConstant += dimensionLow[i/2];
            }

            ArrayConstant = -ArrayConstant;

            //Console.Out.WriteLine($"[VarArray]::{string.Join(",", dimensionSizes)}");
        }

        public override string Serialize()
        {
            return $"VARARRAY,{name},{(int)type},{address},{dimensionSizes.Length},{string.Join(",", dimensionSizes)},{string.Join(",", dimensionLow)}";
        }

        public static VarArray Deserialize(string input, out int size)
        {
            VarArray varArray = VarArray.Deserialize(input.Split(','), 1, out size);
            size += 1;
            return varArray;
        }

        public static VarArray Deserialize(string[] splits, int offset, out int size)
        {
            string name = splits[offset];
            MyType type = (MyType)int.Parse(splits[1 + offset]);
            int address = int.Parse(splits[2 + offset]);
            int dimSize = int.Parse(splits[3 + offset]);
            int[] dimensionLowHigh = new int[dimSize*2];

            for (int i = 4; i < 4 + dimSize; ++i)
            {
                dimensionLowHigh[(i - 4)*2] = int.Parse(splits[i + dimSize + offset]);
                dimensionLowHigh[(i - 4)*2 + 1] = dimensionLowHigh[(i - 4)*2] + int.Parse(splits[i + offset]) - 1;
            }

            size = 4 + dimSize * 2;
            //Console.Out.WriteLine($"[VarArray::Deserialize]-{splits[offset]}, {splits[offset+1]}, {splits[offset+2]}, {splits[offset+3]}, STEP={size}");
            return new VarArray(name, type, address, dimensionLowHigh);
        }
    }

    // Func Row es para poder representar una funcion. (tiene un nombre, type y un address) type viene siendo lo que regresa
    // y al address significa la location donde la funcion empieza (quinto cuadruple es donde la funcion empeiza)
    // 
    public class FuncRow : VarRow
    {
        public override int totalSize
        {
            get
            {
                return parameters.TotalSize() + localVars.TotalSize();
            }
        }

        public VarTable parameters {get; private set;}
        public VarTable localVars {get; private set;}

        public FuncRow(string name, MyType type, int address, VarTable paramList, VarTable locals)
            : base(name, type, address)
        {
            this.parameters = paramList;
            this.localVars = locals;
        }

        public override void SetAddress(int address)
        {
            parameters.UpdateStartAddress(address);
            localVars.UpdateStartAddress(address + parameters.TotalSize());
        }

        public void SetFuncStart(int address)
        {
            this.address = address;
        }

        public override string Serialize()
        {
            return $"FUNCROW,{name},{(int)type},{address},{parameters.Serialize()},{localVars.Serialize()}";
        }

        public static FuncRow Deserialize(ILogger logger, string input)
        {
            int unused;
            return FuncRow.Deserialize(logger, input.Split(','), 1, out unused);
        }

        public static FuncRow Deserialize(ILogger logger, string[] splits, int offset, out int step)
        {
            //Console.Out.WriteLine($"[FuncRow::Deserialize[S]]-{splits[offset]}, {splits[offset+1]}, {splits[offset+2]}");
            string name = splits[offset];
            MyType type = (MyType)int.Parse(splits[offset+1]);
            int address = int.Parse(splits[offset+2]);
            int paramStep;
            VarTable parameters = VarTable.Deserialize(logger, splits, offset+3, out paramStep);
            int localStep;
            VarTable localVars = VarTable.Deserialize(logger, splits, offset+3+paramStep, out localStep);

            step = paramStep + localStep + 3;
            //Console.Out.WriteLine($"[FuncRow::Deserialize[E]]-{splits[offset]}, {splits[offset+1]}, {splits[offset+2]}, STEP={step}");
            return new FuncRow(name, type, address, parameters, localVars);
        }
    }

    public class VarTable : AbstractDataTable<VarRow>
    {
        public VarTable(ILogger logger, int addressStart)
            : base(logger, addressStart) { }

        public static VarTable Deserialize(ILogger logger, string input)
        {
            int unused;
            return VarTable.Deserialize(logger, input.Split(','), 0, out unused);
        }

        public static VarTable Deserialize(ILogger logger, string[] splits, int offset, out int step)
        {
            //Console.Out.WriteLine($"[VarTable::Deserialize[S]]-{splits[offset]}, {splits[offset+1]}, {splits[offset+2]}");
            int addressStart = int.Parse(splits[offset]);
            int addressNext = int.Parse(splits[offset+1]);
            int dataCount = int.Parse(splits[offset+2]);
            VarTable table = new VarTable(logger, addressStart);
            int currentIndex = offset+3;
            while (dataCount > 0)
            {
                if (splits[currentIndex] == "VARARRAY")
                {
                    int substep = 0;
                    VarArray varArray = VarArray.Deserialize(splits, currentIndex+1, out substep);
                    table.AddData(varArray);
                    currentIndex += 1 + substep;
                }
                else
                {
                    VarRow varRow = VarRow.Deserialize(splits, currentIndex+1);
                    table.AddData(varRow);
                    currentIndex += 4;
                }
                
                --dataCount;
            }

            step = currentIndex - offset;

            //Console.Out.WriteLine($"[VarTable::Deserialize[E]]-{splits[offset]}, {splits[offset+1]}, {splits[offset+2]}, STEP={step}");
            return table;
        }
    }

    public class FuncTable : AbstractDataTable<FuncRow>
    {
        public FuncTable(ILogger logger, int addressStart)
            : base(logger, addressStart) { }

        public static FuncTable Deserialize(ILogger logger, string input)
        {
            int unused;
            return FuncTable.Deserialize(logger, input.Split(','), 0, out unused);
        }

        public FuncRow NewFuncRow(string name, MyType type, int address, VarTable paramList, VarTable locals)
        {
            FuncRow row = new FuncRow(name, type, address, paramList, locals);
            row.SetAddress(addressNext);
            return row;
        }

        public void UpdateAddressNext(VarTable locals)
        {
            addressNext = locals.addressNext;
        }

        public static FuncTable Deserialize(ILogger logger, string[] splits, int offset, out int step)
        {
            int addressStart = int.Parse(splits[offset]);
            int addressNext = int.Parse(splits[offset+1]);
            int dataCount = int.Parse(splits[offset+2]);
            //Console.Out.WriteLine($"[FuncTable::Deserialize[S]]-{splits[offset]}, {splits[offset+1]}, {splits[offset+2]}");

            FuncTable table = new FuncTable(logger, addressStart);
            int currentIndex = offset+3;
            while (dataCount > 0)
            {
                int substep;
                FuncRow funcRow = FuncRow.Deserialize(logger, splits, currentIndex+1, out substep);
                table.AddData(funcRow, true);
                currentIndex += substep+1;
                --dataCount;
            }

            step = currentIndex - offset;
            //Console.Out.WriteLine($"[FuncTable::Deserialize[E]]-{splits[offset]}, {splits[offset+1]}, {splits[offset+2]}");
            return table;
        }
    }

    //representa una tabla de variables o funciones 
    public abstract class AbstractDataTable<T> where T: VarRow
    {
        //data es para buscar variables por nombre
        private Dictionary<string, T> data;
        //datalist es la lista de variables para iterar en ellas
        private List<T> dataList;
        private ILogger logger;
        public int addressStart;
        public int addressNext;

        public AbstractDataTable(ILogger logger, int addressStart)
        {
            this.data = new Dictionary<string, T>();
            this.dataList = new List<T>();
            this.logger = logger;
            this.addressStart = addressStart;
            this.addressNext = addressStart;
        }

        public string Serialize()
        {
            return $"{addressStart},{addressNext},{dataList.Count}{(dataList.Count > 0 ? "," : "")}{string.Join(",", dataList.Select(x => x.Serialize()))}";
        }

        public int TotalSize()
        {
            return addressNext - addressStart;
        }

        //que tantas variables hay en tu tabla 
        public int RowCount()
        {
            return dataList.Count;
        }

        public void AddData(T t, bool deserialize = false)
        {
            // Si el nombre ya existe le tengo que regresar un error.
            if (data.ContainsKey(t.name))
            {
                logger.SemErr($"var/func {t.name} ya existe.");
                return;
            }

            if (!deserialize)
                t.SetAddress(addressNext);
            
            data[t.name] = t;
            dataList.Add(t);
            addressNext += t.totalSize;
        }

        // Checa si este VarTable tiene un var de name
        public bool CheckName(string name)
        {
            return data.ContainsKey(name);
        }

        public bool TryFindByAddress(int address, out T t)
        {
            t = null;

            foreach (T data in GetDataList())
            {
                if (data.address == address)
                {
                    t = data;
                    break;
                }
                else if (data is VarArray && address >= data.address && address < (data.address + data.totalSize))
                {
                    t = data;
                    break;
                }
            }

            return t != null;
        }
        // obtiene la informacion de var de name
        public T Find(string name)
        {
            if (!data.ContainsKey(name))
            {
                logger.SemErr($"var/func {name} no existe");
                return null;
            }

            return data[name];
        }

        public void UpdateStartAddress(int addressNewStart)
        {
            foreach (T t in data.Values)
            {
                int offset = t.address - addressStart;
                t.SetAddress(addressNewStart + offset);
            }

            this.addressNext = (addressNext - addressStart) + addressNewStart;
            this.addressStart = addressNewStart;
        }

        public List<T> GetDataList()
        {
            return dataList;
        }

        public AbstractDataTable<T> CombineDataTable(AbstractDataTable<T> otherTable)
        {
            foreach (T t in otherTable.GetDataList())
            {
                this.AddData(t);
            }

            return this;
        }
    }
}