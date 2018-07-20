using JetBrains.Annotations;

using lib.Models;

namespace lib.Commands
{
    public class Flip : BaseCommand
    {
        [NotNull]
        public override byte[] Encode()
        {
            return new byte[] {0b11111101};
        }

        public override void Apply([NotNull] MutableState mutableState, [NotNull] Bot bot)
        {
            mutableState.Harmonics = mutableState.Harmonics == Harmonics.High ? Harmonics.Low : Harmonics.High;
        }
    }
}