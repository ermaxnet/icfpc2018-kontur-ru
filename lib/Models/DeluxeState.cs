using System;
using System.Collections.Generic;
using System.Linq;

using lib.Commands;
using lib.Utils;

namespace lib.Models
{
    public class DeluxeState
    {
        public Matrix SourceMatrix { get; }
        public Matrix TargetMatrix { get; }
        public ComponentTrackingMatrix Matrix { get; }
        public HashSet<Bot> Bots { get; }
        public long Energy { get; set; }
        public Harmonics Harmonics { get; set; }
        public Dictionary<Vec, (Bot bot, ICommand command)> VolatileCells { get; } = new Dictionary<Vec, (Bot, ICommand)>();

        private readonly Dictionary<Bot, ICommand> botCommands = new Dictionary<Bot, ICommand>();
        private readonly Dictionary<Region, (bool isFill, Dictionary<Vec, Bot> corners)> groupRegions = new Dictionary<Region, (bool isFill, Dictionary<Vec, Bot> corners)>();

        public DeluxeState(Matrix sourceMatrix, Matrix targetMatrix)
        {
            SourceMatrix = sourceMatrix ?? new Matrix(targetMatrix.R);
            TargetMatrix = targetMatrix ?? new Matrix(sourceMatrix.R);
            Matrix = new ComponentTrackingMatrix(SourceMatrix.Clone());
            Bots = new HashSet<Bot> { new Bot { Bid = 1, Position = Vec.Zero, Seeds = Enumerable.Range(2, 39).ToList() } };
            Energy = 0;
        }

        public void StartTick()
        {
            Energy += Harmonics == Harmonics.High
                          ? 30 * TargetMatrix.R * TargetMatrix.R * TargetMatrix.R
                          : 3 * TargetMatrix.R * TargetMatrix.R * TargetMatrix.R;
            Energy += 20 * Bots.Count;
            botCommands.Clear();
            VolatileCells.Clear();
            groupRegions.Clear();
        }

        public void SetBotCommand(Bot bot, ICommand command)
        {
            if (!Bots.Contains(bot))
                throw new InvalidOperationException($"Unknown bot {bot}; Command: {command}");
            if (botCommands.TryGetValue(bot, out var duplicateCommand))
                throw new InvalidOperationException($"Bot {bot} has duplicate commands {command} and {duplicateCommand}");
            botCommands.Add(bot, command);

            if (!command.AllPositionsAreValid(Matrix, bot))
                throw new InvalidOperationException($"Incorrect command {command}");

            if (command is GroupCommand groupCommand)
            {
                var region = groupCommand.GetRegion(bot.Position);
                Dictionary<Vec, Bot> corners;
                if (!groupRegions.TryGetValue(region, out var others))
                {
                    groupRegions.Add(region, (command is GFill, corners = new Dictionary<Vec, Bot>()));
                    AddVolatileCells(bot, command, region);
                }
                else
                {
                    corners = others.corners;
                    if (others.isFill && !(command is GFill))
                        throw new InvalidOperationException($"Common volatile region {region}. " +
                                                            $"Bots: {bot}; {string.Join("; ", corners.Values)}. " +
                                                            $"Command: {command};");
                }
                var corner = bot.Position + groupCommand.NearShift;
                if (corners.TryGetValue(corner, out var conflictingBot))
                    throw new InvalidOperationException($"Common group region cell {corner}. " +
                                                        $"Bots: {bot}; {conflictingBot}.");
                corners.Add(corner, bot);
            }

            AddVolatileCells(bot, command, command.GetVolatileCells(bot));
        }

        public List<ICommand> EndTick()
        {
            foreach (var bot in Bots)
            {
                if (!botCommands.ContainsKey(bot))
                    SetBotCommand(bot, new Wait());
            }

            foreach (var kvp in groupRegions)
            {
                var region = kvp.Key;
                var cornersCount = 1 << region.Dim;
                if (kvp.Value.corners.Count != cornersCount)
                    throw new InvalidOperationException($"Not enough bots to construct region {region} for {(kvp.Value.isFill ? nameof(GFill) : nameof(GVoid))}. " +
                                                        $"Bots: {string.Join("; ", kvp.Value.corners.Values)}");
                var bot = kvp.Value.corners.First().Value;
                botCommands[bot].Apply(this, bot);
            }

            // todo check fusionS and fusionP and fisson

            foreach (var kvp in botCommands)
            {
                if (kvp.Value is GroupCommand)
                    continue;
                if (kvp.Value is Halt)
                {
                    if (Bots.Count > 1)
                        throw new InvalidOperationException($"Couldn't halt. Too many bots left: {string.Join("; ", Bots)}");
                    if (Harmonics != Harmonics.Low)
                        throw new InvalidOperationException("Couldn't halt in high harmonics");
                }
                kvp.Value.Apply(this, kvp.Key);
            }

            EnsureWellFormed();
            return botCommands.OrderBy(kvp => kvp.Key.Bid).Select(kvp => kvp.Value).ToList();
        }


        private void AddVolatileCells(Bot bot, ICommand command, IEnumerable<Vec> volatileCells)
        {
            foreach (var vec in volatileCells)
            {
                if (VolatileCells.TryGetValue(vec, out var conflict))
                    throw new InvalidOperationException($"Common volatile cell {vec}. " +
                                                        $"Bots: {bot}; {conflict.bot}. " +
                                                        $"Commands: {command}; {conflict.command}");
                VolatileCells.Add(vec, (bot, command));
            }
        }

        private void EnsureWellFormed()
        {
            if (Harmonics == Harmonics.Low && Matrix.HasNonGroundedVoxels)
                throw new InvalidOperationException("Low Harmonics while non grounded voxel present");
        }
    }
}