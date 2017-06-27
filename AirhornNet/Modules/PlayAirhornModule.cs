using System.Threading.Tasks;
using AirhornNet.Services;
using Discord;
using Discord.Commands;
using log4net;

namespace AirhornNet.Modules
{
    public class PlayAirhornModule : ModuleBase
    {
        AudioService audioService;
        ILog log;

        public PlayAirhornModule(AudioService audioService, ILog log)
        {
            this.audioService = audioService;
            this.log = log;
        }

        [Command("!airhorn", RunMode = RunMode.Async)]
        public async Task AirhornCmd()
        {
            // すでに再生中なら処理しない
            if (audioService.IsJoinedAudio(Context.Guild))
            {
                return;
            }

            await Join();
            await Play("data/airhorn.wav");
            await Task.Delay(500);
            await Leave();
        }

        private async Task Join()
        {
            await audioService.JoinAudio(Context.Guild, (Context.User as IVoiceState).VoiceChannel);
        }

        private async Task Leave()
        {
            await audioService.LeaveAudio(Context.Guild);
        }

        private async Task Play(string filepath)
        {
            try
            {
                await audioService.SendAudioAsync(Context.Guild, Context.Channel, filepath);
            }
            catch (System.Exception e)
            {
                log.Error(e.Message, e);
            }
        }
    }
}
