using Dem_v2;

namespace Demodulador_WinForm_1;

internal static class DscFrameTrimmer
{
    private const int SymbolBits = 10;
    private const int ConsecutiveEosRequired = 3;

    public static string TrimAfterFinalEos(string? rawBits, out string detail)
    {
        if (string.IsNullOrEmpty(rawBits))
        {
            detail = "filtro EOS: cadena vacía";
            return string.Empty;
        }

        for (int bitOffset = 0; bitOffset < SymbolBits; bitOffset++)
        {
            int symbolCount = (rawBits.Length - bitOffset) / SymbolBits;
            if (symbolCount < ConsecutiveEosRequired + 1)
                continue;

            bool extensionDetected = false;

            for (int symbolIndex = 0;
                 symbolIndex + ConsecutiveEosRequired <= symbolCount;
                 symbolIndex++)
            {
                if (!TryDecode(rawBits, bitOffset, symbolIndex, out int eos) ||
                    !IsEos(eos) ||
                    !TryDecode(rawBits, bitOffset, symbolIndex + 1, out int eos2) ||
                    eos2 != eos ||
                    !TryDecode(rawBits, bitOffset, symbolIndex + 2, out int eos3) ||
                    eos3 != eos)
                {
                    continue;
                }

                // Después de los tres EOS queda la repetición RX del ECC. Se conserva,
                // porque Procesamiento.VerificarECC() la utiliza como último símbolo.
                int eccRxIndex = symbolIndex + ConsecutiveEosRequired;
                if (eccRxIndex >= symbolCount)
                    continue;

                // Una extensión DSC comienza inmediatamente después de ese ECC y usa
                // uno de los especificadores de información admitidos (100 a 106).
                int extensionIndex = eccRxIndex + 1;
                if (extensionIndex < symbolCount &&
                    TryDecode(rawBits, bitOffset, extensionIndex, out int extensionSpecifier) &&
                    IsExtensionSpecifier(extensionSpecifier))
                {
                    extensionDetected = true;
                    symbolIndex = extensionIndex;
                    continue;
                }

                int trimEndBit = bitOffset + (eccRxIndex + 1) * SymbolBits;
                int removedBits = rawBits.Length - trimEndBit;
                detail =
                    $"filtro EOS: EOS={eos}, offset={bitOffset}, " +
                    $"extensión={(extensionDetected ? "sí" : "no")}, descartados={removedBits} bits";
                return rawBits.Substring(0, trimEndBit);
            }
        }

        detail = "filtro EOS: no se encontró un final completo";
        return rawBits;
    }

    private static bool TryDecode(string rawBits, int bitOffset, int symbolIndex, out int value)
    {
        int start = bitOffset + symbolIndex * SymbolBits;
        if (start < 0 || start + SymbolBits > rawBits.Length)
        {
            value = 0;
            return false;
        }

        return Decodificador.TryDeco(rawBits.Substring(start, SymbolBits), out value);
    }

    private static bool IsEos(int value)
    {
        return value == 127 || value == 122 || value == 117;
    }

    private static bool IsExtensionSpecifier(int value)
    {
        return value >= 100 && value <= 106;
    }
}
