using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Maths
{
    public class Complex
    {
        #region Instanz

        #region Variablen

        double real, imag, rad, ang;
        /// <summary>
        /// Der Realteil der Komplexen Zahl
        /// </summary>
        public double Real
        {
            get { return real; }
            set
            {
                real = value;
                setAngleView();
            }
        }
        /// <summary>
        /// Der Imaginärteil der Komplexen Zahl
        /// </summary>
        public double Imaginary
        {
            get { return imag; }
            set
            {
                imag = value;
                setAngleView();
            }
        }
        /// <summary>
        /// Der Radius der Komplexen Zahl
        /// </summary>
        public double Radius
        {
            get { return rad; }
            set
            {
                rad = value;
                setArithmView();
            }
        }
        /// <summary>
        /// Der Winkel der Komplexen Zahl
        /// </summary>
        public double Angle
        {
            get { return ang; }
            set
            {
                value = value % (2 * System.Math.PI);
                if (value < 0) value += 2 * System.Math.PI;
                ang = value;
                setArithmView();
            }
        }

        #region Helfer für Variablen

        /// <summary>
        /// Rechnet <see cref="rad"/> und <see cref="ang"/> aus.
        /// </summary>
        private void setAngleView()
        {
            rad = System.Math.Sqrt(real * real + imag * imag);
            if (rad == 0)
            {
                ang = 0;
                return;
            }
            var acos = real / rad;
            var asin = imag / rad;
            var sin = System.Math.Asin(System.Math.Abs(asin));

            if (asin >= 0 && acos >= 0)
                ang = sin;
            else if (asin >= 0 && acos < 0)
                ang = System.Math.PI - sin;
            else if (asin < 0 && acos < 0)
                ang = System.Math.PI + sin;
            else ang = System.Math.PI * 2 - sin;

        }
        /// <summary>
        /// Rechnet <see cref="real"/> und <see cref="imag"/> aus.
        /// </summary>
        private void setArithmView()
        {
            real = rad * System.Math.Cos(ang);
            imag = rad * System.Math.Sin(ang);
            if (rad == 0) ang = 0;
        }

        #endregion

        #endregion

        #region Konstruktoren

        /// <summary>
        /// Erstellt eine neue komplexe Zahl.
        /// </summary>
        public Complex() { }
        /// <summary>
        /// Erstellt eine neue komplexe Zahl.
        /// </summary>
        /// <param name="real">Der Realteil der neuen komplexen Zahl.</param>
        public Complex(double real)
        {
            this.real = real;
            this.imag = 0;
            rad = System.Math.Abs(real);
            ang = real < 0 ? System.Math.PI : 0;
        }
        /// <summary>
        /// Erstellt eine neue komplexe Zahl. Je nachdem, wie isAngleView definiert wurde, werden die Parameter in der 
        /// Winkeldarstellung (true) oder in der arithmetischen Darstellung (false) gedeutet.
        /// </summary>
        /// <param name="real">Wenn isAngleView=true, dann der Radius, ansonsten der Realteil der neuen Komplexen Zahl.</param>
        /// <param name="imag">Wenn isAngleView=true, dann der Winkel, ansonsten der Imaginär der neuen Komplexen Zahl.</param>
        /// <param name="isAngleView">Bestimmt die Deutung der Parameter.</param>
        public Complex(double real, double imag, bool isAngleView = false)
        {
            if (isAngleView)
            {
                this.rad = real;
                this.ang = imag;
                setArithmView();
            }
            else
            {
                this.real = real;
                this.imag = imag;
                setAngleView();
            }
        }

        #endregion

        #region Instanzmethoden

        public override bool Equals(object obj)
        {
            if (obj is Complex) return this == (Complex)obj;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return real.ToString() + (imag < 0 ? "" : "+") + imag.ToString() + "i";
        }

        public string ToString(string format)
        {
            return real.ToString(format) + (imag < 0 ? "" : "+") + imag.ToString(format) + "i";
        }

        #endregion

        #endregion

        #region Klasse

        #region Operatoren

        #region Operatoren nur mit Complex

        public static Complex operator +(Complex c1, Complex c2)
        {
            return new Complex(c1.real + c2.real, c1.imag + c2.imag);
        }

        public static Complex operator -(Complex c)
        {
            return new Complex(-c.real, -c.imag);
        }

        public static Complex operator -(Complex c1, Complex c2)
        {
            return c1 + (-c2);
        }

        public static Complex operator *(Complex c1, Complex c2)
        {
            return new Complex(c1.real * c2.real - c1.imag * c2.imag, c1.real * c2.imag + c1.imag * c2.real);
        }

        public static Complex operator /(Complex c1, Complex c2)
        {
            var d = c2.real * c2.real + c2.imag * c2.imag;
            return new Complex((c1.real * c2.real + c1.imag * c2.imag) / 2, (c1.imag * c2.real - c1.real * c2.imag) / d);
        }

        #endregion

        #region Operatoren Complex und Double

        public static Complex operator +(Complex c, double z)
        {
            return c + (new Complex(z));
        }

        public static Complex operator +(double z, Complex c)
        {
            return c + (new Complex(z));
        }

        public static Complex operator -(Complex c, double z)
        {
            return c - (new Complex(z));
        }

        public static Complex operator -(double z, Complex c)
        {
            return (new Complex(z)) - c;
        }

        public static Complex operator *(Complex c, double z)
        {
            return c * (new Complex(z));
        }

        public static Complex operator *(double z, Complex c)
        {
            return c * (new Complex(z));
        }

        public static Complex operator /(Complex c, double z)
        {
            return c / (new Complex(z));
        }

        public static Complex operator /(double z, Complex c)
        {
            return (new Complex(z)) / c;
        }

        #endregion

        #region Vergleichsoperatoren nur mit Complex

        public static bool operator ==(Complex c1, Complex c2)
        {
            if (((object)c1) == null && ((object)c2) == null) return true;
            if (((object)c1) == null || ((object)c2) == null) return false;
            return c1.real == c2.real && c1.imag == c2.imag;
        }

        public static bool operator !=(Complex c1, Complex c2)
        {
            return !(c1 == c2);
        }

        #endregion

        #region Vergleichsoperatoren Complex und Double

        public static bool operator ==(Complex c, double z)
        {
            if (c == null) return false;
            return c.imag == 0 && c.real == z;
        }

        public static bool operator ==(double z, Complex c)
        {
            if (c == null) return false;
            return c.imag == 0 && c.real == z;
        }

        public static bool operator !=(Complex c, double z)
        {
            return !(c == z);
        }

        public static bool operator !=(double z, Complex c)
        {
            return !(c == z);
        }

        #endregion

        #endregion

        #region Konvertierungen

        public static implicit operator Complex(double z)
        {
            return new Complex(z);
        }

        public static explicit operator double(Complex c)
        {
            if (c == null) throw new ArgumentNullException();
            if (c.imag != 0) throw new ArithmeticException();
            return c.real;
        }

        #endregion

        #region Methoden

        #region Alias für Operatoren

        #region Operatoren nur mit Complex

        public static Complex Add(Complex c1, Complex c2)
        {
            return c1 + c2;
        }

        public static Complex Subtract(Complex c1, Complex c2)
        {
            return c1 - c2;
        }

        public static Complex Negate(Complex c)
        {
            return -c;
        }

        public static Complex Multiplicate(Complex c1, Complex c2)
        {
            return c1 * c2;
        }

        public static Complex Divide(Complex c1, Complex c2)
        {
            return c1 / c2;
        }

        #endregion

        #region Operatoren Complex und Double

        public static Complex Add(Complex c, double z)
        {
            return c + z;
        }

        public static Complex Subtract(Complex c, double z)
        {
            return c - z;
        }

        public static Complex Subtract(double z, Complex c)
        {
            return z - c;
        }

        public static Complex Multiplicate(Complex c, double z)
        {
            return c * z;
        }

        public static Complex Divide(Complex c, double z)
        {
            return c / z;
        }

        public static Complex Divide(double z, Complex c)
        {
            return z / c;
        }

        #endregion

        #endregion

        public static Complex Conjugate(Complex c)
        {
            return new Complex(c.real, -c.imag);
        }

        public static Complex Pow(Complex c, double z)
        {
            return new Complex(System.Math.Pow(c.rad, z), z * c.ang, true);
        }

        public static Complex[] Root(Complex c, int z)
        {
            if (z <= 0) throw new ArgumentOutOfRangeException("z");
            var r = new Complex[z];
            for (var i = 0; i < z; ++i)
                r[i] = new Complex(System.Math.Pow(c.rad, 1.0 / z), (c.ang + 2 * i * System.Math.PI) / z, true);
            return r;
        }

        #endregion

        #endregion
    }
}
