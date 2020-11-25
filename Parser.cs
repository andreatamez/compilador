using System.Collections; // esto incluye las listas y diccionarios
using System.Collections.Generic; // listas y diccionarios pero soporta genericas que significa tener una lista de ints o string
using System.Linq; // incluye las librerias que permite ling support

/*PG 35 en User Manual Taste.ATG ejemplo */
/* Aqui es donde comienza el codigo de COCO */


using System;

namespace MeMySelf {



public class Parser {
	public const int _EOF = 0;
	public const int _cte_i = 1;
	public const int _cte_f = 2;
	public const int _cte_c = 3;
	public const int _id = 4;
	public const int _letrero = 5;
	public const int maxT = 48;

	const bool _T = true;
	const bool _x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

public string progName;

    private const string IntReturnFunc = "__intReturn";
    private const string FloatReturnFunc = "__floatReturn";
    private const string CharReturnFunc = "__charReturn";

    public CodeGenerator codegen;

    public CuboSemantico cuboSemantico;
    public bool DebugMode;

    private Stack<Op> pOper = new Stack<Op>();
    private Stack<MyType> pilaType = new Stack<MyType>();
    private Stack<int> pilaOp = new Stack<int>(); //Operador
    private Stack<int> pSaltos = new Stack<int>(); //pila Saltos para llenar el quad

    /*Cuadruplos */
    /*Lo que hace esta funcion es que al momento en que llaman verifica que el orden de operaciones
    este correcto para crear un cuadruplo 
    
    ej. voy a hacer una multiplicacion y verifica que si funcione bien 

    5 * 6 * 7...

    pilaOp = 5,6
    pOper = *,*

    ahora que hay un * mas checa AddQuad con *,/ que son el mismo orden de operaciones
    entonces ahora llama esto y ahora checa okey hay un * o division y si si hay ahora 
    toma lo que hay en la pilaOp lo toma y lo guarda en t1 y crea ese cuadruplo y pushea t1 
    a la stack

    pilaOp = t1
    pOper = ...
    
    */
    public void CheckAndAddQuad(params Op[] ops)
    {
        Op peekOp = pOper.Count == 0 ? Op.EMPTY : pOper.Peek();
        if(ops.Any(op => op.Equals(peekOp)) && pilaOp.Count >= 2)
        {
            if (DebugMode) Console.Out.WriteLine($"peekOp={peekOp}, pOper.Count={pOper.Count}, pilaOp.Count={pilaOp.Count}, pilaType.Count={pilaType.Count}");
            Op op = pOper.Pop();
            int oper_der = pilaOp.Pop();
            int oper_izq = pilaOp.Pop();

            MyType tipo_der = pilaType.Pop();
            MyType tipo_izq = pilaType.Pop();

            MyType tipo_out = cuboSemantico.GetType(op, tipo_izq, tipo_der);
            if (tipo_out == MyType.error)
            {
                SemErr($"Type mismatch. op={op}, tipo_izq={tipo_izq}, tipo_der={tipo_der}");
            }

            int registro = codegen.NextRegister(tipo_out);
            codegen.AddQuad(op, oper_izq, oper_der, registro);

            pilaOp.Push(registro);
            pilaType.Push(tipo_out);
        }
    }

    /*Asignacion = checo el siguiente caracter a ver si es equals  */
    public bool IsAsignacion()
    {
        Token next = scanner.Peek();
        return la.kind == _id && next.val == "=";
    }

    /*Parametro */
    public bool IsParam()
    {
        Token next = scanner.Peek();
        return la.kind == _id && next.val == ":";
    }

    /*Declaracion de Variable */
    public bool IsVarDeclaration()
    {
        Token next = scanner.Peek();
        return next.val == ":";
    }

/**********************/
    /* NO ESTA COMPLETA AUN funciones especiales para graficas*/
    public void InitFuncionesEspeciales()
    {
        VarTable emptyLocals = codegen.GetEmptyTable();

        VarTable lineFuncParams = codegen.GetEmptyTable();
        // lineFuncParams.AddData(new VarRow())
        // FuncRow lineFunc = new FuncRow("LINE", MyType.undef, 0, )
    }

/**********************/

/*2.3.1 */


	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void MeMySelf() {
		VarTable table = codegen.GetEmptyTable(); 
		if (la.kind == 6) {
			Get();
		} else if (la.kind == 7) {
			Get();
		} else SynErr(49);
		Expect(4);
		progName = t.val; /*Andrea */
		int count = codegen.AddQuad(Op.GOTO, -1, -1, -1); /*GOTO */
		pSaltos.Push(count); /*pSaltos agregamos el 0 */
		
		Expect(8);
		if (la.kind == 12) {
			DefinicionDeVariables(out table);
		}
		table.AddData(new VarRow(IntReturnFunc, MyType.intType, 0));
		table.AddData(new VarRow(FloatReturnFunc, MyType.floatType, 0));
		table.AddData(new VarRow(CharReturnFunc, MyType.charType, 0));
		FuncRow prog = codegen.funcTable.NewFuncRow(progName, MyType.undef, 0, table, codegen.GetEmptyTable()); // new FuncRow(progName, MyType.undef, 0, table, codegen.GetEmptyTable());
		/*estas son para cuando la gente va retornar custom global variables
		para que yo pueda hacer returns funcionar para asignarle esa variable
		y se utiliza porque sino no hay manera de regresar de funciones. */
		/*Aqui asignamos la tabla con los datos */
		codegen.AddFunction(prog);
		codegen.SetGlobalTable(table);
		
		while (StartOf(1)) {
			DefinicionDeFuncion();
		}
		Expect(9);
		Expect(10);
		int jumpLoc = codegen.Cont(); 
		int gotoOp = pSaltos.Pop();
		codegen.Fill(gotoOp, jumpLoc);
		prog.localVars.UpdateStartAddress(codegen.funcTable.addressNext);
		codegen.SetCurrentScopeTable(prog.localVars); 
		if (StartOf(2)) {
			Estatutos();
		}
		codegen.funcTable.UpdateAddressNext(prog.localVars); 
		Expect(11);
	}

	void DefinicionDeVariables(out VarTable table) {
		MyType type; List<VarRow> list; table = codegen.GetEmptyTable(); 
		Expect(12);
		tipo(out type);
		Expect(13);
		lista_ids(ref type, out list);
		Expect(8);
		foreach (var row in list)
		{
		   //esto se puede ver en symbol table donde le damos un nombre type and size
		   table.AddData(row);
		} 
		while (IsVarDeclaration()) {
			tipo(out type);
			Expect(13);
			lista_ids(ref type, out list);
			Expect(8);
			foreach (var row in list)
			{
			   table.AddData(row);
			} 
		}
	}

	void DefinicionDeFuncion() {
		MyType type = MyType.undef; VarTable parameters, localVars; 
		if (StartOf(3)) {
			tipo_retorno(out type);
		}
		Expect(21);
		Expect(4);
		string funcName = t.val;
		parameters = codegen.GetEmptyTable();
		localVars = codegen.GetEmptyTable(); 
		Expect(22);
		if (la.kind == 14 || la.kind == 15 || la.kind == 16) {
			Parametros(out parameters);
		}
		Expect(23);
		if (la.kind == 12) {
			DefinicionDeVariables(out localVars);
		}
		int curQuad = codegen.Cont(); /*current quad en el que estamos */
		FuncRow row = codegen.funcTable.NewFuncRow(funcName, type, curQuad, parameters, localVars); // new FuncRow(funcName, type, curQuad, parameters, localVars);
		/*Currentscope son las local variables para cualquier seccion en la que estemos */
		codegen.AddFunction(row);
		codegen.SetCurrentScopeParamsTable(parameters);
		codegen.SetCurrentScopeTable(localVars); 
		Expect(10);
		if (StartOf(2)) {
			Estatutos();
		}
		Expect(11);
		codegen.UpdateFuncTableAddressing(localVars);
		codegen.ResetCurrentScopeTable();
		codegen.AddQuad(Op.ENDFUNC, -1, -1, -1); 
	}

	void Estatutos() {
		Estatuto();
		while (StartOf(2)) {
			Estatuto();
		}
	}

	void tipo(out MyType type) {
		type = MyType.error; 
		if (la.kind == 14) {
			Get();
			type = MyType.intType; 
		} else if (la.kind == 15) {
			Get();
			type = MyType.floatType; 
		} else if (la.kind == 16) {
			Get();
			type = MyType.charType; 
		} else SynErr(50);
	}

	void lista_ids(ref MyType type, out List<VarRow> rows ) {
		rows = new List<VarRow>(); VarArray varArray = null; string idName; 
		Expect(4);
		idName = t.val; 
		if (la.kind == 18) {
			ArrayDeclaration(ref idName, ref type, out varArray);
			rows.Add(varArray); 
		}
		if (varArray == null)
		{
		   rows.Add(new VarRow(idName, type, 0));
		}
		
		varArray = null;
		
		while (la.kind == 17) {
			Get();
			Expect(4);
			idName = t.val; 
			if (la.kind == 18) {
				ArrayDeclaration(ref idName, ref type, out varArray);
				rows.Add(varArray); 
			}
			if (varArray == null)
			{
			   rows.Add(new VarRow(idName, type, 0));
			}
			
			varArray = null;
			
		}
	}

	void ArrayDeclaration(ref string idName, ref MyType type, out VarArray varArray) {
		List<int> dims = new List<int>(); string low, high; int lowI, highI; varArray = null; 
		Expect(18);
		Expect(1);
		low = t.val; high = null; 
		if (la.kind == 19) {
			Get();
			Expect(1);
			high = t.val; 
		}
		Expect(20);
		if (!int.TryParse(low, out lowI))
		{
		   SemErr($"arrays necesita int: {low}");
		   return;
		}
		
		if (high == null)
		{
		   high = (lowI - 1).ToString();
		   lowI = 0;
		}
		
		if (!int.TryParse(high, out highI))
		{
		   SemErr($"arrays necesita int: {high}");
		   return;
		}
		
		dims.Add(lowI);
		dims.Add(highI);
		
		
		while (la.kind == 18) {
			Get();
			Expect(1);
			low = t.val; high = null; 
			if (la.kind == 19) {
				Get();
				Expect(1);
				high = t.val; 
			}
			Expect(20);
			if (!int.TryParse(low, out lowI))
			{
			   SemErr($"arrays necesita int: {low}");
			   return;
			}
			
			if (high == null)
			{
			   high = (lowI - 1).ToString();
			   lowI = 0;
			}
			
			if (!int.TryParse(high, out highI))
			{
			   SemErr($"arrays necesita int: {high}");
			   return;
			}
			
			dims.Add(lowI);
			dims.Add(highI);
			
			
		}
		varArray = new VarArray(idName, type, 0, dims.ToArray()); 
	}

	void tipo_retorno(out MyType type) {
		type = MyType.error; 
		if (la.kind == 24) {
			Get();
			type = MyType.undef; 
		} else if (la.kind == 14 || la.kind == 15 || la.kind == 16) {
			tipo(out type);
		} else SynErr(51);
	}

	void Parametros(out VarTable parameters) {
		parameters = codegen.GetEmptyTable(); VarRow row; 
		var_simple(out row);
		parameters.AddData(row); 
		while (la.kind == 17) {
			Get();
			var_simple(out row);
			parameters.AddData(row); 
		}
	}

	void var_simple(out VarRow row) {
		MyType type; 
		tipo(out type);
		Expect(4);
		row = new VarRow(t.val, type, 0); 
	}

	void Estatuto() {
		MyType type; 
		if (IsAsignacion()) {
			Asignacion();
		} else if (la.kind == 4) {
			LlamadaAVoid();
		} else if (la.kind == 26) {
			RetornoDeFuncion(out type);
		} else if (la.kind == 27) {
			Lectura();
		} else if (la.kind == 28) {
			Escritura();
		} else if (la.kind == 29) {
			EstatutoDeDecision();
		} else if (la.kind == 32) {
			Condicional_Rep();
		} else if (la.kind == 34) {
			NoCondicional_Rep();
		} else SynErr(52);
	}

	void Asignacion() {
		MyType type; VarArray arrayVar = null; int derefAddr = -1; 
		Expect(4);
		VarRow row = codegen.GetVar(t.val); 
		if (la.kind == 18) {
			ArrayIndexing(ref arrayVar, out derefAddr);
		}
		Expect(25);
		Expresion(out type);
		type = pilaType.Pop();
		var exp = pilaOp.Pop();
		//verifica que si asignas entre estas cosas cual es el outType
		MyType tipo_out = cuboSemantico.GetType(Op.SET, row.type, type);
		if (tipo_out == MyType.error)
		{
		   SemErr("tipo invalido");
		}
		
		codegen.AddQuad(Op.SET, exp, -1, row.address);
		
		Expect(8);
	}

	void LlamadaAVoid() {
		Expect(4);
		FuncRow row = codegen.GetFunction(t.val);
		codegen.AddQuad(Op.ERA, row.address, -1, -1);    
		Expect(22);
		if (StartOf(4)) {
			ParametrosConValores(ref row);
		}
		Expect(23);
		codegen.AddQuad(Op.GOSUB, -1, -1, row.address); 
		Expect(8);
	}

	void RetornoDeFuncion(out MyType type) {
		Expect(26);
		Expect(22);
		Expresion(out type);
		type = pilaType.Pop();
		int exp = pilaOp.Pop();
		string retName = null;
		switch (type)
		{
		   case MyType.intType:
		       retName = IntReturnFunc;
		       break;
		   case MyType.floatType:
		       retName = FloatReturnFunc;
		       break;
		   case MyType.charType:
		       retName = CharReturnFunc;
		       break;
		   default:
		       SemErr($"Tipo invalido: {type}");
		       break;
		}
		
		var row = codegen.GetVar(retName);
		
		codegen.AddQuad(Op.RETURN, exp, row.address, -1); 
		Expect(23);
		Expect(8);
	}

	void Lectura() {
		VarRow row; 
		Expect(27);
		Expect(22);
		Expect(4);
		row = codegen.GetVar(t.val);
		if (row.IsArray)
		{
		   SemErr($"{t.val} es array, no puede leer");
		   return;
		}
		codegen.AddQuad(Op.READ, row.address, -1, -1); 
		while (la.kind == 17) {
			Get();
			Expect(4);
			row = codegen.GetVar(t.val);
			if (row.IsArray)
			{
			   SemErr($"{t.val} es array, no puede leer");
			   return;
			}
			codegen.AddQuad(Op.READ, row.address, -1, -1); 
		}
		Expect(23);
		Expect(8);
	}

	void Escritura() {
		MyType type; int exp; MyType unusedType; 
		Expect(28);
		Expect(22);
		Expresion(out type);
		exp = pilaOp.Pop();
		unusedType = pilaType.Pop();
		codegen.AddQuad(Op.WRITE, exp, -1, -1);
		
		while (la.kind == 17) {
			Get();
			Expresion(out type);
			exp = pilaOp.Pop();
			unusedType = pilaType.Pop();
			codegen.AddQuad(Op.WRITE, exp, -1, -1);
			
		}
		Expect(23);
		Expect(8);
	}

	void EstatutoDeDecision() {
		MyType type; 
		Expect(29);
		Expect(22);
		Expresion(out type);
		if (type != MyType.intType)
		{
		   SemErr("Requiere expresion condicional (intType)");
		}
		
		pilaType.Pop();
		int exp = pilaOp.Pop();
		int quadNum = codegen.AddQuad(Op.GOTOF, exp, -1, -1);
		pSaltos.Push(quadNum);
		
		Expect(23);
		Expect(30);
		Expect(10);
		if (StartOf(2)) {
			Estatutos();
		}
		int endIf = codegen.AddQuad(Op.GOTO, -1, -1, -1);
		int jumpLine = pSaltos.Pop();
		codegen.Fill(jumpLine, endIf + 1);
		pSaltos.Push(endIf);
		
		Expect(11);
		if (la.kind == 31) {
			Get();
			Expect(10);
			if (StartOf(2)) {
				Estatutos();
			}
			Expect(11);
		}
		int nextQuad = codegen.Cont();
		int endIf2 = pSaltos.Pop();
		codegen.Fill(endIf2, nextQuad);
		
	}

	void Condicional_Rep() {
		MyType type; 
		Expect(32);
		Expect(22);
		pSaltos.Push(codegen.Cont()); 
		Expresion(out type);
		MyType expType = pilaType.Pop();
		if (expType != MyType.intType)
		{
		    SemErr("Requiere expresion condicional (intType)");
		}
		
		int exp = pilaOp.Pop();
		int jumpQuad = codegen.AddQuad(Op.GOTOF, exp, -1, -1);
		pSaltos.Push(jumpQuad); 
		Expect(23);
		Expect(33);
		Expect(10);
		if (StartOf(2)) {
			Estatutos();
		}
		int jumpQuad2 = pSaltos.Pop(); 
		int cicloEmpieza = pSaltos.Pop(); 
		int skipQuad = codegen.AddQuad(Op.GOTO, -1, -1, cicloEmpieza);
		codegen.Fill(jumpQuad2, skipQuad); 
		Expect(11);
		Expect(8);
	}

	void NoCondicional_Rep() {
		MyType type; 
		Expect(34);
		Expect(4);
		VarRow iter = codegen.GetVar(t.val);
		if (iter.type != MyType.intType || iter.IsArray)
		{
		   SemErr($"Requiere expresion intType, pero {t.val} es {iter.type},IsArray:{iter.IsArray}");
		}
		
		Expect(25);
		Expresion(out type);
		type = pilaType.Pop();
		int exp = pilaOp.Pop();
		if (type != MyType.intType)
		{
		   SemErr($"Requiere expresion intType, pero {exp} es {type}");
		}
		
		codegen.AddQuad(Op.SET, exp, -1, iter.address);
		
		
		Expect(35);
		Expresion(out type);
		type = pilaType.Pop();
		exp = pilaOp.Pop();
		if (type != MyType.intType)
		{
		   SemErr($"Requiere expresion intType, pero {exp} es {type}");
		}
		
		int terminaReg = codegen.NextRegister(type);
		codegen.AddQuad(Op.SET, exp, -1, terminaReg);
		
		int cmpReg = codegen.NextRegister(MyType.intType);
		int cmpQuad = codegen.AddQuad(Op.LTE, iter.address, terminaReg, cmpReg);
		int jumpQuad = codegen.AddQuad(Op.GOTOF, cmpReg, -1, -1);
		pSaltos.Push(cmpQuad);
		pSaltos.Push(jumpQuad);
		
		Expect(33);
		Expect(10);
		if (StartOf(2)) {
			Estatutos();
		}
		Expect(11);
		int cnstAddress = codegen.AddConstanteInt("1");
		codegen.AddQuad(Op.ADD, cnstAddress, iter.address, iter.address);
		jumpQuad = pSaltos.Pop();
		cmpQuad = pSaltos.Pop();
		codegen.AddQuad(Op.GOTO, -1, -1, cmpQuad);
		
		int nextQuad = codegen.Cont();
		codegen.Fill(jumpQuad, nextQuad);
		
	}

	void ArrayIndexing(ref VarArray varArray, out int refAddress) {
		MyType type; int counter = 0, constAddr; int reg = codegen.NextRegister(MyType.intType); 
		Expect(18);
		if (counter == varArray.dimensionSizes.Length)
		{
		   SemErr($"Invalid tamano(grande): {counter}");
		}
		
		Expresion(out type);
		type = pilaType.Pop(); 
		int exp = pilaOp.Pop();
		if (type != MyType.intType)
		{
		   SemErr($"Index de Array debe ser intType: {type}");
		}
		
		codegen.AddQuad(Op.VER, exp, varArray.dimensionLow[counter], varArray.dimensionLow[counter] + varArray.dimensionSizes[counter] - 1);
		if (counter == 0)
		{
		   codegen.AddQuad(Op.SET, exp, -1, reg);
		}
		else
		{
		   constAddr = codegen.AddConstanteInt(varArray.dimensionSizes[counter].ToString());
		   codegen.AddQuad(Op.MUL, reg, constAddr, reg);
		   codegen.AddQuad(Op.ADD, reg, exp, reg);
		}
		
		
		++counter; 
		Expect(20);
		while (la.kind == 18) {
			Get();
			if (counter == varArray.dimensionSizes.Length)
			{
			   SemErr($"Invalid tamano(grande): {counter}");
			}
			
			Expresion(out type);
			type = pilaType.Pop(); 
			exp = pilaOp.Pop();
			if (type != MyType.intType)
			{
			   SemErr($"Index de Array debe ser intType: {type}");
			}
			
			codegen.AddQuad(Op.VER, exp, varArray.dimensionLow[counter], varArray.dimensionLow[counter] + varArray.dimensionSizes[counter] - 1);
			if (counter == 0)
			{
			   codegen.AddQuad(Op.SET, exp, -1, reg);
			}
			else
			{
			   constAddr = codegen.AddConstanteInt(varArray.dimensionSizes[counter].ToString());
			   codegen.AddQuad(Op.MUL, reg, constAddr, reg);
			   codegen.AddQuad(Op.ADD, reg, exp, reg);
			}
			
			
			++counter; 
			Expect(20);
		}
		if (counter != varArray.dimensionSizes.Length)
		{
		   SemErr($"Invalid tamano(pequeno): {counter}");
		}
		
		constAddr = codegen.AddConstanteInt(varArray.ArrayConstant.ToString());
		codegen.AddQuad(Op.ADD, reg, constAddr, reg);
		
		int nextReg = codegen.NextRegister(MyType.intType);
		codegen.AddQuad(Op.VARADDRESS, varArray.address, -1, nextReg);
		codegen.AddQuad(Op.ADD, reg, nextReg, reg);
		
		refAddress = reg;
		
	}

	void Expresion(out MyType type) {
		OR_Exp(out type);
		while (la.kind == 36) {
			Get();
			pOper.Push(Op.OR); 
			OR_Exp(out type);
			CheckAndAddQuad(Op.OR); 
		}
	}

	void ParametrosConValores(ref FuncRow row) {
		MyType type; int exp; VarRow rowCmp;
		List<VarRow> parameters = row.parameters.GetDataList();
		int counter = 0;
		string idName = null;
		bool needsIdLabel = false;
		bool hasIdLabel = false;        
		if (IsParam()) {
			Expect(4);
			hasIdLabel = true; needsIdLabel = true; idName = t.val; 
			Expect(13);
		}
		Expresion(out type);
		type = pilaType.Pop();
		exp = pilaOp.Pop();
		rowCmp = parameters[counter];
		if (hasIdLabel && !rowCmp.name.Equals(idName))
		{
		   SemErr($"calling func={row.name}, id={idName} does not match {rowCmp.name}, pos {counter}");
		}
		
		if (rowCmp.type != type)
		{
		   SemErr($"calling func={row.name}, in type {type} does not match expected type {rowCmp.type}");
		}
		
		codegen.AddQuad(Op.PARAMETER, exp, row.address, counter);
		counter += 1;
		
		hasIdLabel = false;
		idName = null; 
		while (la.kind == 17) {
			Get();
			if (IsParam()) {
				Expect(4);
				hasIdLabel = true; needsIdLabel = true; idName = t.val; 
				Expect(13);
			}
			if (needsIdLabel && !hasIdLabel)
			{
			  SemErr($"Need id label after having id label."); 
			}
			Expresion(out type);
			type = pilaType.Pop();
			exp = pilaOp.Pop();
			rowCmp = parameters[counter];
			if (hasIdLabel && !rowCmp.name.Equals(idName))
			{
			   SemErr($"calling func={row.name}, id={idName} does not match {rowCmp.name}, pos {counter}");
			}
			
			if (rowCmp.type != type)
			{
			   SemErr($"calling func={row.name}, in type {type} does not match expected type {rowCmp.type}");
			}
			
			codegen.AddQuad(Op.PARAMETER, exp, row.address, counter);
			counter += 1;
			
			hasIdLabel = false;
			idName = null; 
		}
		if (counter != parameters.Count)
		{
		   SemErr($"calling func={row.name}, incorrect number of parameters");
		}
	}

	void OR_Exp(out MyType type) {
		AND_Exp(out type);
		while (la.kind == 37) {
			Get();
			pOper.Push(Op.AND); 
			AND_Exp(out type);
			CheckAndAddQuad(Op.AND); 
		}
	}

	void AND_Exp(out MyType type) {
		Op op; 
		EQ_Exp(out type);
		if (la.kind == 38 || la.kind == 39) {
			if (la.kind == 38) {
				Get();
				op = Op.EQU; 
			} else {
				Get();
				op = Op.NEQ; 
			}
			pOper.Push(op); 
			EQ_Exp(out type);
			CheckAndAddQuad(Op.EQU, Op.NEQ); 
		}
	}

	void EQ_Exp(out MyType type) {
		Op op; 
		CMP_Exp(out type);
		if (StartOf(5)) {
			if (la.kind == 40) {
				Get();
				op = Op.GT; 
			} else if (la.kind == 41) {
				Get();
				op = Op.GTE; 
			} else if (la.kind == 42) {
				Get();
				op = Op.LT; 
			} else {
				Get();
				op = Op.LTE; 
			}
			pOper.Push(op); 
			CMP_Exp(out type);
			CheckAndAddQuad(Op.GT, Op.GTE, Op.LT, Op.LTE); 
		}
	}

	void CMP_Exp(out MyType type) {
		Op op; 
		Term(out type);
		while (la.kind == 44 || la.kind == 45) {
			if (la.kind == 44) {
				Get();
				op = Op.ADD; 
			} else {
				Get();
				op = Op.SUB; 
			}
			pOper.Push(op); 
			Term(out type);
			CheckAndAddQuad(Op.ADD, Op.SUB); 
		}
	}

	void Term(out MyType type) {
		Op op; 
		Factor(out type);
		while (la.kind == 46 || la.kind == 47) {
			if (la.kind == 46) {
				Get();
				op = Op.MUL; 
			} else {
				Get();
				op = Op.DIV; 
			}
			pOper.Push(op); 
			Factor(out type);
			CheckAndAddQuad(Op.MUL, Op.DIV); 
		}
	}

	void Factor(out MyType type) {
		type = MyType.undef; int address; 
		switch (la.kind) {
		case 22: {
			Get();
			pOper.Push(Op.PAREN); 
			Expresion(out type);
			Expect(23);
			if (pOper.Peek() != Op.PAREN)
			{
			   SemErr("Paren mismatch");
			}
			else
			{
			   pOper.Pop();
			}
			
			break;
		}
		case 1: {
			Get();
			type = MyType.intType; pilaType.Push(type); 
			address = codegen.AddConstanteInt(t.val);
			pilaOp.Push(address); 
			break;
		}
		case 2: {
			Get();
			type = MyType.floatType; pilaType.Push(type);
			address = codegen.AddConstanteFloat(t.val);
			pilaOp.Push(address); 
			break;
		}
		case 3: {
			Get();
			type = MyType.charType; pilaType.Push(type);
			address = codegen.AddConstanteChar(t.val);
			pilaOp.Push(address); 
			break;
		}
		case 5: {
			Get();
			type = MyType.undef; pilaType.Push(type);
			address = codegen.AddConstanteLetrero(t.val);
			pilaOp.Push(address); 
			break;
		}
		case 4: {
			Get();
			string idName = t.val; bool isFunc = false, isArray = false; 
			if (la.kind == 18 || la.kind == 22) {
				if (la.kind == 22) {
					Get();
					FuncRow row = codegen.GetFunction(idName);
					if (!MyTypeUtil.IsValidReturnType(row.type))
					{
					   SemErr($"Func {t.val} tiene tipo retorno invalido = {row.type}");
					}
					
					codegen.AddQuad(Op.ERA, row.address, -1, -1);
					isFunc = true;    
					if (StartOf(4)) {
						ParametrosConValores(ref row);
					}
					codegen.AddQuad(Op.GOSUB, -1, -1, row.address); 
					var reg = codegen.NextRegister(row.type);
					string retName = null;
					switch (row.type)
					{
					   case MyType.intType:
					       retName = IntReturnFunc;
					       break;
					   case MyType.floatType:
					       retName = FloatReturnFunc;
					       break;
					   case MyType.charType:
					       retName = CharReturnFunc;
					       break;
					   default:
					       SemErr($"tipo invalido {row.type}");
					       break;
					}
					
					VarRow vRow = codegen.GetVar(retName);
					
					codegen.AddQuad(Op.SET, vRow.address, -1, reg);
					pilaType.Push(row.type);
					pilaOp.Push(reg); 
					Expect(23);
				} else {
					VarArray arrayVar = codegen.GetArray(idName);
					isArray = true;
					int derefAddr; 
					ArrayIndexing(ref arrayVar, out derefAddr);
					int nextReg = codegen.NextRegister(arrayVar.type);
					codegen.AddQuad(Op.DEREF, derefAddr, -1, nextReg);
					pilaType.Push(arrayVar.type); pilaOp.Push(nextReg); 
				}
			}
			if (!isFunc && !isArray)
			{
			   VarRow varRow = codegen.GetVar(idName);
			   if (varRow.IsArray)
			   {
			       SemErr($"{idName} es array, falta [ ] ");
			       return;
			   }
			
			   type = varRow.type;
			   pilaType.Push(type); pilaOp.Push(varRow.address); 
			}
			   
			break;
		}
		default: SynErr(53); break;
		}
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		MeMySelf();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_x,_x,_x, _x,_T,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_T,_x,_x, _T,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_T,_T, _x,_x,_x,_x, _x,_x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
	public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public virtual void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "cte_i expected"; break;
			case 2: s = "cte_f expected"; break;
			case 3: s = "cte_c expected"; break;
			case 4: s = "id expected"; break;
			case 5: s = "letrero expected"; break;
			case 6: s = "\"Program\" expected"; break;
			case 7: s = "\"program\" expected"; break;
			case 8: s = "\";\" expected"; break;
			case 9: s = "\"main()\" expected"; break;
			case 10: s = "\"{\" expected"; break;
			case 11: s = "\"}\" expected"; break;
			case 12: s = "\"var\" expected"; break;
			case 13: s = "\":\" expected"; break;
			case 14: s = "\"int\" expected"; break;
			case 15: s = "\"float\" expected"; break;
			case 16: s = "\"char\" expected"; break;
			case 17: s = "\",\" expected"; break;
			case 18: s = "\"[\" expected"; break;
			case 19: s = "\"..\" expected"; break;
			case 20: s = "\"]\" expected"; break;
			case 21: s = "\"module\" expected"; break;
			case 22: s = "\"(\" expected"; break;
			case 23: s = "\")\" expected"; break;
			case 24: s = "\"void\" expected"; break;
			case 25: s = "\"=\" expected"; break;
			case 26: s = "\"return\" expected"; break;
			case 27: s = "\"read\" expected"; break;
			case 28: s = "\"write\" expected"; break;
			case 29: s = "\"if\" expected"; break;
			case 30: s = "\"then\" expected"; break;
			case 31: s = "\"else\" expected"; break;
			case 32: s = "\"while\" expected"; break;
			case 33: s = "\"do\" expected"; break;
			case 34: s = "\"from\" expected"; break;
			case 35: s = "\"to\" expected"; break;
			case 36: s = "\"|\" expected"; break;
			case 37: s = "\"&\" expected"; break;
			case 38: s = "\"==\" expected"; break;
			case 39: s = "\"!=\" expected"; break;
			case 40: s = "\">\" expected"; break;
			case 41: s = "\">=\" expected"; break;
			case 42: s = "\"<\" expected"; break;
			case 43: s = "\"<=\" expected"; break;
			case 44: s = "\"+\" expected"; break;
			case 45: s = "\"-\" expected"; break;
			case 46: s = "\"*\" expected"; break;
			case 47: s = "\"/\" expected"; break;
			case 48: s = "??? expected"; break;
			case 49: s = "invalid MeMySelf"; break;
			case 50: s = "invalid tipo"; break;
			case 51: s = "invalid tipo_retorno"; break;
			case 52: s = "invalid Estatuto"; break;
			case 53: s = "invalid Factor"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public virtual void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
	
	public virtual void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}
	
	public virtual void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}
	
	public virtual void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}
}