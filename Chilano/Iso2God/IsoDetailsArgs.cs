namespace Chilano.Iso2God
{
   internal class IsoDetailsArgs
    {
        public string PathISO;
        public string PathTemp;

        public IsoDetailsArgs(string ISO, string Temp)
        {
            PathISO = ISO;
            PathTemp = Temp;
        }
    }
}

