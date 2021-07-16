namespace Fountain
{
    using Matt.GaussianElimination;

    class StepCountingProblem : IGaussianProblem
    {
        readonly IGaussianProblem _problem;

        public StepCountingProblem(IGaussianProblem problem)
        {
            _problem = problem;
        }

        bool IGaussianProblem.HasCoefficient(int row, int coefficient)
        {
            return _problem.HasCoefficient(row, coefficient);
        }

        void IGaussianProblem.Xor(int from, int to)
        {
            _problem.Xor(from, to);
            NumSteps++;
        }

        int IGaussianProblem.NumCoefficients => _problem.NumCoefficients;

        int IGaussianProblem.NumRows => _problem.NumRows;

        public long NumSteps { get; private set;}
    }
}