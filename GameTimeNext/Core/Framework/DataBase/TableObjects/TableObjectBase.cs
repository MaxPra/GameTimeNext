using System;

namespace GameTimeNext.Core.Framework.DataBase.TableObjects
{
    public abstract class TableObjectBase
    {
        private string _originalSignature;

        protected TableObjectBase()
        {
            _originalSignature = string.Empty;
        }

        public bool HasChanged()
        {
            string currentSignature = BuildSignature();

            return !string.Equals(
                currentSignature,
                _originalSignature,
                StringComparison.Ordinal
            );
        }

        public void AcceptChanges()
        {
            _originalSignature = BuildSignature();
        }

        protected abstract string BuildSignature();

        protected string NormalizeString(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value;
        }

        protected string NormalizeDateTime(DateTime value)
        {
            return value.Ticks.ToString();
        }

        protected string NormalizeDouble(double value)
        {
            long bits = BitConverter.DoubleToInt64Bits(value);
            return bits.ToString();
        }

        protected string NormalizeLong(long value)
        {
            return value.ToString();
        }
    }
}
