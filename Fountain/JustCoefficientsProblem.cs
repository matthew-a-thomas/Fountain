namespace Fountain
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Matt.Accelerated;
    using Matt.GaussianElimination;

    class JustCoefficientsProblem : IGaussianProblem
    {
        readonly List<Memory<bool>> _coefficients = new();

        public JustCoefficientsProblem(int numCoefficients)
        {
            NumCoefficients = numCoefficients;
        }

        public void Add(ReadOnlyMemory<bool> coefficients)
        {
            if (coefficients.Length != NumCoefficients)
                throw new Exception("Wrong number of coefficients");
            _coefficients.Add(coefficients.ToArray());
        }

        public bool HasCoefficient(int row, int coefficient) => _coefficients[row].Span[coefficient];

        public void Xor(int from, int to)
        {
            Bitwise.Xor(
                MemoryMarshal.AsBytes(_coefficients[from].Span),
                MemoryMarshal.AsBytes(_coefficients[to].Span)
            );
        }

        public int NumCoefficients { get; }
        public int NumRows => _coefficients.Count;
    }
}