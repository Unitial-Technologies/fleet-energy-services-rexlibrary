using System;

namespace MDF4xx.Blocks
{
    partial class CCBlock : BaseBlock
    {
        private delegate double DCalculate(double input);

        DCalculate Calculate;

        double CalcIdentical(double input) => input;

        double A => cc_val[0].AsDouble;
        double B => cc_val[1].AsDouble;
        double C => cc_val[2].AsDouble;
        double D => cc_val[3].AsDouble;
        double E => cc_val[4].AsDouble;
        double F => cc_val[5].AsDouble;

        // Linear functions for calculating based on if Param is 0
        double CalcLinear_00(double input) => 0;
        double CalcLinear_0X(double input) => B * input;
        double CalcLinear_X0(double input) => A;
        double CalcLinear_XX(double input) => B * input + A;

        DCalculate GetLinearCalc(int map)
        {
            switch (map)
            {
                case 00: return CalcLinear_00;
                case 01: return CalcLinear_0X;
                case 10: return CalcLinear_X0;
                default: return CalcLinear_XX;
            }
        }

        DCalculate GetCalcMethod()
        {
            switch (ConvertType)
            {
                case ConversionType.Identical: return CalcIdentical;
                case ConversionType.Linear: return GetLinearCalc((Convert.ToByte(A == 0) << 1) | Convert.ToByte(B == 0));
                case ConversionType.Rational: return null;
                case ConversionType.Formula: return null;
                case ConversionType.tblValueToValueInt: return null;
                case ConversionType.tblValueToValue: return null;
                case ConversionType.tblRangeToVal: return null;
                case ConversionType.tblValueToText: return null;
                case ConversionType.tblRangeToText: return null;
                case ConversionType.tblTextToValue: return null;
                case ConversionType.tblTextToText: return null;
                case ConversionType.tblBitfieldText: return null;
                default: return null;
            }
        }

        void UpdateConvertMethod()
        {
            Calculate = GetCalcMethod();
        }
    }
}
