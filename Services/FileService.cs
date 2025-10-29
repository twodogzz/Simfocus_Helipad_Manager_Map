using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Simfocus_Helipad_Manager_Map.Services
{
    // Minimal undo stack item
    record RenameOperation(string From, string To, string? BackupPath);

    public class FileService : IFileService
    {
        private readonly Stack<RenameOperation> _undoStack = new();

        public bool IsFileInUse(string path)
        {
            try
            {
                using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch
            {
                return true;
            }
        }

        public async Task<(bool Success, string Message)> ToggleEnableAsync(string filePath, bool backupBeforeRename, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return (false, "No file path provided");
            if (!File.Exists(filePath)) return (false, "File doesn't exist");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var lower = filePath.ToLowerInvariant();
                bool currentlyDisabled = lower.EndsWith(".off") || lower.EndsWith(".bgl.off");
                string target;
                if (currentlyDisabled)
                {
                    // remove trailing .off
                    if (filePath.EndsWith(".bgl.off", StringComparison.OrdinalIgnoreCase))
                        target = filePath.Substring(0, filePath.Length - ".off".Length);
                    else if (filePath.EndsWith(".off", StringComparison.OrdinalIgnoreCase))
                        target = filePath.Substring(0, filePath.Length - ".off".Length);
                    else
                        target = filePath;
                }
                else
                {
                    // append .OFF
                    target = filePath + ".OFF";
                }

                string? backupPath = null;
                if (backupBeforeRename)
                {
                    var dir = Path.GetDirectoryName(filePath) ?? Path.GetTempPath();
                    var name = Path.GetFileName(filePath);
                    backupPath = Path.Combine(dir, $"{name}.bak_{DateTime.UtcNow:yyyyMMddHHmmssfff}");
                    await Task.Run(() => File.Copy(filePath, backupPath), cancellationToken);
                }

                // Perform atomic move inside same volume, overwrite not expected.
                await Task.Run(() => File.Move(filePath, target), cancellationToken);

                // Push undo
                _undoStack.Push(new RenameOperation(target, filePath, backupPath));

                return (true, $"Renamed '{Path.GetFileName(filePath)}' -> '{Path.GetFileName(target)}'");
            }
            catch (IOException ioex)
            {
                return (false, $"IO error: {ioex.Message}");
            }
            catch (UnauthorizedAccessException ua)
            {
                return (false, $"Permission error: {ua.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public Task<(bool Success, string Message)> UndoLastAsync(CancellationToken cancellationToken = default)
        {
            if (_undoStack.Count == 0) return Task.FromResult((false, "Nothing to undo"));

            var op = _undoStack.Pop();
            try
            {
                if (!File.Exists(op.From)) return Task.FromResult((false, "File to undo does not exist"));

                File.Move(op.From, op.To);
                // Optionally: restore backup? We keep backup if present.
                return Task.FromResult((true, $"Undo: {Path.GetFileName(op.From)} -> {Path.GetFileName(op.To)}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult((false, $"Undo failed: {ex.Message}"));
            }
        }
    }
}