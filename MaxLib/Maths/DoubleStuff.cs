namespace MaxLib.Maths
{
    public class DoubleMatrix : Matrix<double>
    {
        public DoubleMatrix(int width, int height)
            : base(width, height)
        {

        }

        public DoubleMatrix(double[,] data)
            : base(data)
        {

        }

        protected override double One
        {
            get { return 1; }
        }

        protected override double Zero
        {
            get { return 0; }
        }

        protected override double Add(double value1, double value2)
        {
            return value1 + value2;
        }

        protected override double Negate(double value)
        {
            return -value;
        }

        protected override double Multiplicate(double value1, double value2)
        {
            return value1 * value2;
        }

        protected override double Divide(double value1, double value2)
        {
            return value1 / value2;
        }

        protected override Matrix<double> CreateMatrix(double[,] data)
        {
            return new DoubleMatrix(data);
        }

        protected override Determinat<double> CreateDeterminat(double[,] data)
        {
            if (Width != Height) return null;
            return new DoubleDeterminate(data);
        }
    }

    public class DoubleDeterminate : Determinat<double>
    {
        public DoubleDeterminate(int size)
            : base(size)
        {

        }

        public DoubleDeterminate(double[,] data)
            : base(data)
        {

        }

        protected override double One
        {
            get { return 1; }
        }

        protected override double Zero
        {
            get { return 0; }
        }

        protected override double Add(double value1, double value2)
        {
            return value1 + value2;
        }

        protected override double Negate(double value)
        {
            return -value;
        }

        protected override double Multiplicate(double value1, double value2)
        {
            return value1 * value2;
        }

        protected override double Divide(double value1, double value2)
        {
            return value1 / value2;
        }

        protected override Matrix<double> CreateMatrix(double[,] data)
        {
            return new DoubleMatrix(data);
        }

        protected override Determinat<double> CreateDeterminat(double[,] data)
        {
            return new DoubleDeterminate(data);
        }
    }
}
