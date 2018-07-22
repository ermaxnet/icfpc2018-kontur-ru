using System;

using lib.Commands;
using lib.Models;
using lib.Primitives;
using lib.Strategies.Features.Async;
using lib.Utils;

namespace lib.Strategies.Features
{
    public class FillVoxel : SimpleSingleBotStrategyBase
    {
        private readonly Vec whatToFill;
        private readonly Vec fromPos;
        private readonly Action afterFillVoxel;

        public FillVoxel(DeluxeState state, Bot bot, Vec whatToFill, Vec fromPos, Action afterFillVoxel = null)
            : base(state, bot)
        {
            this.whatToFill = whatToFill;
            this.fromPos = fromPos;
            this.afterFillVoxel = afterFillVoxel;
        }

        protected override async StrategyTask<bool> Run()
        {
            if (bot.Position != fromPos)
            {
                if (!await new MoveSingleBot(state, bot, fromPos))
                    return false;
            }

            var command = new Fill(new NearDifference(whatToFill - fromPos));
            if (command.HasVolatileConflicts(bot, state) || state.Matrix[whatToFill])
                return false;

            afterFillVoxel?.Invoke();

            await Do(command);
            return true;
        }
    }
}