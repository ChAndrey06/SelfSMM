using System.Reflection;
using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Services;

public class TranslateService
{
    private readonly Dictionary<string, Dictionary<LanguageEnum, string>> _dictionary;
    public LanguageEnum Language;

    public TranslateService(LanguageEnum language = LanguageEnum.Eng)
    {
        Language = language;
        
        _dictionary = new Dictionary<string, Dictionary<LanguageEnum, string>>
        {
            { nameof(StartMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Hello, I'm the best service for an instagram's Social Media Manager!" },
                    { LanguageEnum.Rus, "Привет, я лучший помощник SMM-щика!" }
                }
            },
            { nameof(HashtagsByPhoto), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Find hashtags by photo" },
                    { LanguageEnum.Rus, "Подобрать хештеги по фото" }
                }
            },
            { nameof(HashtagsByPhotoInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Don't know what hashtags to match the photo?\nSend the bot an image and it will use it to match your hashtags" },
                    { LanguageEnum.Rus, "Не знаете, какие хештеги подобрать к фото?\nПришлите боту изображение, и на его основе он подберет вам хештеги" }
                }
            },
            { nameof(Hashtags), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Hashtags" },
                    { LanguageEnum.Rus, "Хештеги" }
                }
            },
            { nameof(Presets), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Presets" },
                    { LanguageEnum.Rus, "Пресеты" }
                }
            },
            { nameof(Preset), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Preset" },
                    { LanguageEnum.Rus, "Пресет" }
                }
            },
            { nameof(Apply), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Apply" },
                    { LanguageEnum.Rus, "Применить" }
                }
            },
            { nameof(ServerError), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Server error" },
                    { LanguageEnum.Rus, "Ошибка сервера" }
                }
            },
            { nameof(CreatePost), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "💰Create a post" },
                    { LanguageEnum.Rus, "💰Придумать пост" }
                }
            },
            { nameof(CreatePlan), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "💰Write content plan" },
                    { LanguageEnum.Rus, "💰Написать контент-план" }
                }
            },
            { nameof(DownloadIgTt), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "💰Download from instagram/tiktok" },
                    { LanguageEnum.Rus, "💰Скачать с инстаграм/tiktok" }
                }
            },
            { nameof(DownloadPinterest), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Download from pinterest" },
                    { LanguageEnum.Rus, "Скачать с pinterest" }
                }
            },
            { nameof(DownloadPinterestInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Want to download photos/videos? Send the bot a link and the bot will send back the downloaded material" },
                    { LanguageEnum.Rus, "Хотите скачать фото/видео ? Пришлите боту ссылку, и бот пришлет в ответ скаченные материалы" }
                }
            },
            { nameof(RelatedHashtags), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "💰Find related hashtags" },
                    { LanguageEnum.Rus, "💰Подобрать хештеги" }
                }
            },
            { nameof(RelatedHashtagsInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Send the bot a hashtag, and it will select top hashtags, frequenc, with the ability to save to Excel.\nExample query: #London" },
                    { LanguageEnum.Rus, "Пришлите боту хештег, и он подберет топ хештегов, частотность, с возможностью сохранить в Excel.\nПример запроса: #Москва или Москва" }
                }
            },
            { nameof(CompetitorHashtags), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "💰Competitor hashtags" },
                    { LanguageEnum.Rus, "💰Хештеги конкурентов" }
                }
            },
            { nameof(CompetitorHashtagsInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Send the bot a link to your profile or type your profile name with an @ sign, for example: @selfsmmbot\nBot will show profile information and a menu for viewing hashtags of profile" },
                    { LanguageEnum.Rus, "Пришлите боту ссылку на профиль или напишите через знак @ имя профиля, например: @selfsmmbot\nБот покажет информацию по профилю и меню для просмотра хештегов профиля" }
                }
            },
            { nameof(NeighboringHashtags), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "💰Neighboring hashtags" },
                    { LanguageEnum.Rus, "💰Соседние хештеги" }
                }
            },
            { nameof(NeighboringHashtagsText), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Neighboring hashtags" },
                    { LanguageEnum.Rus, "Соседние хештеги" }
                }
            },
            { nameof(NeighboringHashtagsInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Send a hashtag to the bot, after receiving the list, click on the found button - neighboring hashtags. The bot will pick up the hashtags that are most commonly shared." },
                    { LanguageEnum.Rus, "Пришлите боту хештег, после получения списка нажмите на появившуюся кнопку - соседние хештеги. Бот подберет хештеги, которые наиболее часто используют совместно." }
                }
            },
            { nameof(LineBreak), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Line break" },
                    { LanguageEnum.Rus, "Работа с текстом (пробелы, проверка)" }
                }
            },
            { nameof(VoiceMessageToText), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Voice message to text" },
                    { LanguageEnum.Rus, "Голосовое в текст" }
                }
            },
            { nameof(VoiceMessageToTextInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Are you more comfortable speaking than writing text? Send the bot a voice message and it will return the recognized text" },
                    { LanguageEnum.Rus, "Вам удобней говорить, чем писать текст ? Отправьте боту голосовое сообщение, и он вернет распознанный текст" }
                }
            },
            { nameof(TextDetectionByImage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Text detection by image" },
                    { LanguageEnum.Rus, "Распознать текст с фото" }
                }
            },
            { nameof(DetectText), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Detect text" },
                    { LanguageEnum.Rus, "Распознать текст" }
                }
            },
            { nameof(TextDetectionByImageInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "You need to retype something from a picture? For example, a poem from a book. Take a picture of the text and send it to the bot, then click on the text recognition button that appears, and the bot will send you the text it finds" },
                    { LanguageEnum.Rus, "Нужно что-то перепечатать с картинки ? например, стихотворение из книги. Сфотографируйте текст и отправьте боту, после чего нажмите на появившуюся кнопку распознования текста с фото, и бот отправит вам найденный текст" }
                }
            },
            { nameof(TextToImage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Text to image" },
                    { LanguageEnum.Rus, "Перевести текст в фото" }
                }
            },
            { nameof(PresetsByPhoto), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "💰Presets by photo" },
                    { LanguageEnum.Rus, "💰Пресеты для фото" }
                }
            },
            { nameof(RandomNumberGenerator), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Random number generator" },
                    { LanguageEnum.Rus, "Генератор случайных чисел" }
                }
            },
            { nameof(RandomNumberGeneratorInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "How do I choose a winner in a contest? Use the random number generator by sending the bot a message with number intervals, and the bot will generate a random number in response.\nExample: 0-130" },
                    { LanguageEnum.Rus, "Как выбрать победителя в конкурсе ? Воспользуйтесь генератором случайных чисел, отправив боту сообщение с интервалами чисел, и в ответ бот сгенерирует случайное число.\nПример: 0-130" }
                }
            },
            { nameof(Instagram), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Instagram" },
                    { LanguageEnum.Rus, "Инстаграм" }
                }
            },
            { nameof(TgVk), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Telegram/Vk" },
                    { LanguageEnum.Rus, "Телеграм/Vk" }
                }
            },
            { nameof(ChooseForResource), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Choose for which resource you want to write a post?" },
                    { LanguageEnum.Rus, "Выберите для какого ресурса необходимо написать пост?" }
                }
            },
            { nameof(CreatePostFillTopic), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Tell me which topic should I post about?\n\nExample topics:\n- like a loaded horse\n- is it possible to lose weight without sports\n- relax without money" },
                    { LanguageEnum.Rus, "Напишите, на какую тему мне создать пост ?\n\nПримеры тем:\n- как содержать лошадь\n- можно ли похудеть без спорта\n- отдых без денег" }
                }
            },
            { nameof(LineBreakInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Send the bot the text of the post, and it will return the finished text with line breaks (just copy and paste it into instagram)" },
                    { LanguageEnum.Rus, "Отправьте боту текст поста, и он вернет уже готовый текст с переносом строк (просто скопируйте его и вставьте в пост).\nУверены, что ваш текст корректный ? Проверьте его с помощью бота, так же отправьте текст поста боту, после чего нажмите на появившуюся кнопку проверки текста" }
                }
            },
            { nameof(TextToImageInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Useful for people who make carousel photos from text, or when your text won't fit into a post. Send text to the bot and click the button that appears to convert your text into an image. If you won't add text to your image, send image and text in single message. Or send the bot just a picture and set the default picture background for text" },
                    { LanguageEnum.Rus, "Полезно для тех, кто делает фото-карусели из текста, или когда ваш текст не влезает в пост. Пришлите боту текст и, нажав на появившуюся кнопку, преобразуйте ваш текст в изображение. Если вы хотите наложить текст на собственную картинку, пришлите картинку и текст в одном сообщении. Или пришлите боту только картинку и установите ее фоном для текста по умолчанию" }
                }
            },
            { nameof(DownloadIgTtInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Want to download photos/videos? Send the bot a link and the bot will send back the downloaded material" },
                    { LanguageEnum.Rus, "Хотите скачать фото/видео ? Пришлите боту ссылку, и бот пришлет в ответ скаченные материалы\n\nКомпания Meta, владеющая платформами Instagram и Facebook, признана экстремистской и запрещена на территории РФ." }
                }
            },
            { nameof(SendImage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Send image" },
                    { LanguageEnum.Rus, "Отправьте картинку" }
                }
            },
            { nameof(PleaseWait), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "♻️ Please wait... processing may take several minutes" },
                    { LanguageEnum.Rus, "♻️ Подождите... обработка может занимать несколько минут" }
                }
            },
            { nameof(CreatePlanFillTopic), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Tell me what topic should I create a content plan about?\n\nExample topics:\n- shugaring training for women who want to become shugaring masters\n- health and fitness: exercise, diet, motivation, for the weight loss marathon sale\n- manicure at home, how to find clients" },
                    { LanguageEnum.Rus, "Напишите, на какую тему мне создать контент-план ?\n\nПримеры тем:\n- обучения шугарингу для женщин, желающих стать мастерами шугаринга\n- здоровье и фитнес: упражнения, диета, мотивация для продажи марафона по похудению\n- маникюр на дому, как найти клиентов" }
                }
            },
            { nameof(CreatePlanFillAudience), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Write who is your audience?\n\nExamples:\n- young mothers\n- beauty-conscious women\n- overweight people who want to lose weight easily" },
                    { LanguageEnum.Rus, "Напишите, кто ваша аудитория?\n\nПримеры:\n- молодые мамы\n- женщины, заботящиеся о своей красоте\n- люди с лишним весом, которые хотят легко похудеть" }
                }
            },
            { nameof(Week), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "week" },
                    { LanguageEnum.Rus, "неделя" }
                }
            },
            { nameof(Month), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "month" },
                    { LanguageEnum.Rus, "месяц" }
                }
            },
            { nameof(Yeah), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "year" },
                    { LanguageEnum.Rus, "год" }
                }
            },
            { nameof(CreatePlanFillPeriod), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Choose for which period to write a content plan?" },
                    { LanguageEnum.Rus, "Выберите на какой период написать контент-план ?" }
                }
            },
            { nameof(FillPreset), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "Send preset photo number to choose (1-{0})" },
                    { LanguageEnum.Rus, "Отправьте номер пресета (1-{0})" }
                }
            },
            { nameof(PresetsByPhotoInitMessage), new Dictionary<LanguageEnum, string>
                {
                    { LanguageEnum.Eng, "You no longer need to buy presets and manually process the photo. Send the bot a photo and select a preset you like, the bot will process it and return you a beautiful photo" },
                    { LanguageEnum.Rus, "Вам больше не нужно покупать пресеты и в ручную обрабатывать фото. Отправьте боту фото и выберите понравившийся пресет, бот сам обработает и вернет вам красивое фото" }
                }
            }
        };
    }
    
    public string StartMessage => _dictionary[nameof(StartMessage)][Language];
    public string HashtagsByPhoto => _dictionary[nameof(HashtagsByPhoto)][Language];
    public string HashtagsByPhotoInitMessage => _dictionary[nameof(HashtagsByPhotoInitMessage)][Language];
    public string Hashtags => _dictionary[nameof(Hashtags)][Language];
    public string Presets => _dictionary[nameof(Presets)][Language];
    public string Preset => _dictionary[nameof(Preset)][Language];
    public string Apply => _dictionary[nameof(Apply)][Language];
    public string ServerError => _dictionary[nameof(ServerError)][Language];
    public string CreatePost => _dictionary[nameof(CreatePost)][Language];
    public string CreatePlan => _dictionary[nameof(CreatePlan)][Language];
    public string DownloadIgTt => _dictionary[nameof(DownloadIgTt)][Language];
    public string DownloadPinterest => _dictionary[nameof(DownloadPinterest)][Language];
    public string DownloadPinterestInitMessage => _dictionary[nameof(DownloadPinterestInitMessage)][Language];
    public string RelatedHashtags => _dictionary[nameof(RelatedHashtags)][Language];
    public string RelatedHashtagsInitMessage => _dictionary[nameof(RelatedHashtagsInitMessage)][Language];
    public string CompetitorHashtags => _dictionary[nameof(CompetitorHashtags)][Language];
    public string CompetitorHashtagsInitMessage => _dictionary[nameof(CompetitorHashtagsInitMessage)][Language];
    public string NeighboringHashtags => _dictionary[nameof(NeighboringHashtags)][Language];
    public string NeighboringHashtagsText => _dictionary[nameof(NeighboringHashtagsText)][Language];
    public string NeighboringHashtagsInitMessage => _dictionary[nameof(NeighboringHashtagsInitMessage)][Language];
    public string LineBreak => _dictionary[nameof(LineBreak)][Language];
    public string VoiceMessageToText => _dictionary[nameof(VoiceMessageToText)][Language];
    public string VoiceMessageToTextInitMessage => _dictionary[nameof(VoiceMessageToTextInitMessage)][Language];
    public string TextDetectionByImage => _dictionary[nameof(TextDetectionByImage)][Language];
    public string DetectText => _dictionary[nameof(DetectText)][Language];
    public string TextDetectionByImageInitMessage => _dictionary[nameof(TextDetectionByImageInitMessage)][Language];
    public string TextToImage => _dictionary[nameof(TextToImage)][Language];
    public string PresetsByPhoto => _dictionary[nameof(PresetsByPhoto)][Language];
    public string RandomNumberGenerator => _dictionary[nameof(RandomNumberGenerator)][Language];
    public string RandomNumberGeneratorInitMessage => _dictionary[nameof(RandomNumberGeneratorInitMessage)][Language];
    public string Instagram => _dictionary[nameof(Instagram)][Language];
    public string TgVk => _dictionary[nameof(TgVk)][Language];
    public string ChooseForResource => _dictionary[nameof(ChooseForResource)][Language];
    public string CreatePostFillTopic => _dictionary[nameof(CreatePostFillTopic)][Language];
    public string LineBreakInitMessage => _dictionary[nameof(LineBreakInitMessage)][Language];
    public string TextToImageInitMessage => _dictionary[nameof(TextToImageInitMessage)][Language];
    public string DownloadIgTtInitMessage => _dictionary[nameof(DownloadIgTtInitMessage)][Language];
    public string SendImage => _dictionary[nameof(SendImage)][Language];
    public string PleaseWait => _dictionary[nameof(PleaseWait)][Language];
    public string CreatePlanFillTopic => _dictionary[nameof(CreatePlanFillTopic)][Language];
    public string CreatePlanFillAudience => _dictionary[nameof(CreatePlanFillAudience)][Language];
    public string Week => _dictionary[nameof(Week)][Language];
    public string Month => _dictionary[nameof(Month)][Language];
    public string Yeah => _dictionary[nameof(Yeah)][Language];
    public string CreatePlanFillPeriod => _dictionary[nameof(CreatePlanFillPeriod)][Language];
    public string FillPreset => _dictionary[nameof(FillPreset)][Language];
    public string PresetsByPhotoInitMessage => _dictionary[nameof(PresetsByPhotoInitMessage)][Language];
}