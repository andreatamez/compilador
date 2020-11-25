// Andrea Tamez A01176494
namespace MeMySelf
{
    public class Quadruple
    {
        public Op oper; // first operando
        public int oper_izq; //siguiente
        public int oper_der; //siguiente
        public int target; //resultado

        public Quadruple(Op oper, int oper_izq, int oper_der, int target)
        {
            this.oper = oper;
            this.oper_izq = oper_izq;
            this.oper_der = oper_der;
            this.target = target;
        }

        public override string ToString()
        {
            return $"{oper}, {oper_izq}, {oper_der}, {target}";
        }

        public string Serialize()
        {
            return $"{(int)oper},{oper_izq},{oper_der},{target}";
        }

        public static Quadruple Deserialize(string line)
        {
            string[] splits = line.Split(',');
            Op oper = (Op)int.Parse(splits[0]);
            int oper_izq = int.Parse(splits[1]);
            int oper_der = int.Parse(splits[2]);
            int target = int.Parse(splits[3]);
            return new Quadruple(oper, oper_izq, oper_der, target);
        }
    }
}