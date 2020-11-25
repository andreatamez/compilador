// Andrea Tamez A01176494
// clases de CoCo

//Logger es un tipo contenedor alrededor del parser para que 
//otras clases puedan hablarle a el semantic error del parser
// lo utilizo para poder decirle al parser que algo salio mal
namespace MeMySelf
{
    using System;

    public interface ILogger
    {
        void SemErr(string err);
    }

    // Para los clases que necesitan un logger
    public class Logger : ILogger
    {
        private Parser parser;

        public Logger(Parser parser)
        {
            this.parser = parser;
        }

        public void SemErr(string err)
        {
            parser.SemErr(err);
        }
    }
}