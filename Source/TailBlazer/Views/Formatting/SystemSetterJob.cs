using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Windows;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;
using TailBlazer.Domain.Formatting;
using TailBlazer.Domain.Infrastructure;
using TailBlazer.Domain.Ratings;
using TailBlazer.Domain.Settings;
using System.Linq;

namespace TailBlazer.Views.Formatting
{
    public sealed class SystemSetterJob: IDisposable
    {
        private readonly IDisposable _cleanUp;
        
        public SystemSetterJob(ISetting<GeneralOptions> setting,
            IRatingService ratingService,
            ISchedulerProvider schedulerProvider)
        {
             var themeSetter =  setting.Value.Select(options => options.Theme)
                .DistinctUntilChanged()
                .ObserveOn(schedulerProvider.MainThread)
                .Subscribe(formattingTheme =>
                {
                    var dark = formattingTheme == Domain.Formatting.Theme.Dark;
                    var paletteHelper = new PaletteHelper();
                    var theme = paletteHelper.GetTheme();
                    theme.SetBaseTheme(dark ? MaterialDesignThemes.Wpf.Theme.Dark : MaterialDesignThemes.Wpf.Theme.Light);

                    var name = formattingTheme.GetAccentColor();
                    var swatch = new MaterialDesignColors.SwatchesProvider().Swatches.FirstOrDefault(
                        s => string.Compare(s.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0 && s.IsAccented);
                    theme.SetSecondaryColor(swatch.AccentExemplarHue.Color);

                    paletteHelper.SetTheme(theme);
                });

            var frameRate = ratingService.Metrics
                .Take(1)
                .Select(metrics => metrics.FrameRate)
                .Wait();

            schedulerProvider.MainThread.Schedule(() =>
            {
                Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = frameRate });

            });

            _cleanUp = new CompositeDisposable( themeSetter);
        }


        public void Dispose()
        {
            _cleanUp.Dispose();
        }
    }
}