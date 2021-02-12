using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Services;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Cosmos.Cms.Common.Tests
{
    [TestClass]
    public class TranslationServiceTests
    {
        private static IOptions<GoogleCloudAuthConfig> _googleCloudAuthOptions;

        [ClassInitialize]
        public static void TestInitalize(TestContext context)
        {
            _googleCloudAuthOptions = StaticUtilities.GetGoogleOptions();
        }

        [TestMethod]
        public async Task A_Get_LanguageList_Success()
        {
            var translationServices = new TranslationServices(_googleCloudAuthOptions);

            var list = await translationServices.GetSupportedLanguages();

            Assert.IsTrue(list.Languages.Count > 100);
        }

        [TestMethod]
        public async Task B_TranslateText_Success()
        {
            var translationServices = new TranslationServices(_googleCloudAuthOptions);

            var testString =
                "The other night 'bout two o'clock, or maybe it was three,\r\nAn elephant with shining tusks came chasing after me.\r\nHis trunk was wavin' in the air an'  spoutin' jets of steam\r\nAn' he was out to eat me up, but still I didn't scream\r\nOr let him see that I was scared - a better thought I had,\r\nI just escaped from where I was and crawled in bed with dad.\r\n\r\nSource: https://www.familyfriendpoems.com/poem/being-brave-at-night-by-edgar-albert-guest";

            var result = await translationServices.GetTranslation("es", "en", new[] {testString});

            Assert.IsTrue(result.Translations.Count == 1);

            Assert.AreEqual(result.Translations[0].TranslatedText,
                "La otra noche alrededor de las dos, o tal vez eran las tres, un elefante con colmillos brillantes vino persigui�ndome. Su ba�l se agitaba en el aire y arrojaba chorros de vapor y �l estaba fuera a comerme, pero a�n as� no grit� o le dej� ver que estaba asustado, lo pens� mejor, simplemente escap� desde donde estaba y me arrastr� en la cama con pap�. Fuente: https://www.familyfriendpoems.com/poem/being-brave-at-night-by-edgar-albert-guest");
        }
    }
}