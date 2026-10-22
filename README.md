# Wordrise

منصة خيرية مجانية لتعليم الإنجليزية للمتحدثين بالعربية.

## المجلدات

| المجلد | المحتوى |
|---|---|
| `web/` | الموقع: Angular 22 مع SSR ودعم الاتجاه من اليمين لليسار |
| `api/` | الـ Backend: ASP.NET Core (.NET 10) بمعمارية Modular Monolith |

## تشغيل المنصة كاملة محلياً

تحتاج نافذتين (Terminal):

**1. الـ API** (بدون تثبيت أي شيء، على SQLite):
```bash
cd api
dotnet run --project src/Wordrise.Host --launch-profile https-sqlite
```

**2. الموقع:**
```bash
cd web
npm install
npm start
```

ثم افتح `http://localhost:4200`. الموقع يمرر كل طلبات `/api` إلى الـ API عبر Proxy (`web/proxy.conf.json`)، فيبدوان كموقع واحد ويعمل تسجيل الدخول بالكوكيز.

- `/`: الصفحة الرئيسية، وفيها تجربة درس ببيانات تجريبية بدون تسجيل.
- `/register` و`/login`: إنشاء حساب وتسجيل الدخول.
- `/app`: التطبيق الحقيقي ببيانات الطالب (يتطلب تسجيل الدخول).

أوامر أخرى داخل `web/`: `npm run build` للبناء، و`npx ng test --watch=false` للاختبارات.

## تشغيل الـ API

**مع PostgreSQL (الإعداد الأساسي):** يحتاج Docker.

```bash
docker compose up -d
cd api
dotnet run --project src/Wordrise.Host --launch-profile https
```

**بدون تثبيت أي شيء (SQLite للتطوير فقط):**

```bash
cd api
dotnet run --project src/Wordrise.Host --launch-profile https-sqlite
```

- الـ API على `https://localhost:7163`، ووثائق OpenAPI على `/openapi/v1.json`.
- ملف `src/Wordrise.Host/Wordrise.Host.http` فيه طلبات جاهزة للتجربة من Visual Studio أو VS Code.
- الاختبارات: `dotnet test` داخل `api/`.
- migration جديدة لموديول الهوية:
  `dotnet ef migrations add <Name> --project src/Modules/Identity/Wordrise.Modules.Identity.Core --startup-project src/Modules/Identity/Wordrise.Modules.Identity.Core --output-dir Data/Migrations`

### إعدادات التعلّم (`appsettings.json`)

| المفتاح | الافتراضي | المعنى |
|---|---|---|
| `Learning:TimeZone` | `Asia/Riyadh` | منتصف الليل في هذه المنطقة يبدأ يوماً دراسياً جديداً |
| `Learning:Reviews:Enabled` | `true` | `false` يوقف المراجعة بالكامل: لا كلمات مستحقة، والدرس يبدأ بالكلمات الجديدة |
| `Learning:Reviews:MaxPerSession` | `20` | أقصى عدد كلمات في جلسة المراجعة الواحدة (1 إلى 200) |
| `Learning:Reviews:Backlog:Enabled` | `true` | `false` يسمح بالدروس الجديدة مهما تراكمت المراجعات |
| `Learning:Reviews:Backlog:Limit` | `30` | إذا زادت المستحقات على هذا العدد، يكون اليوم مراجعة فقط (1 إلى 1000) |

القيم خارج المدى توقف التطبيق عند التشغيل برسالة تذكر اسم الإعداد. ويمكن تغيير أي إعداد على السيرفر بمتغير بيئة، مثلاً: `Learning__Reviews__Enabled=false`.

### إنتاج الدروس بالذكاء الاصطناعي (موديول `ContentAuthoring`)

الدورة: تطلب درساً (موقف + خطوة نحو) ← الكاتب (Claude Opus) يكتبه ← فحوص مجانية للشكل ← المراجع (Claude Sonnet) يحكم عليه ← يعيد الكاتب إصلاحه حتى 3 مرات ← يصل إليك للمراجعة ← عند موافقتك يُنشر للمتعلّمين. كل شيء عبر `/api/v1/admin/lessons/*` وللمراجع أو المدير بعد التحقق بخطوتين (2FA) فقط.

**مفتاح Claude لا يوضع في أي ملف.** على السيرفر: متغير البيئة `Authoring__Claude__ApiKey`. وعلى جهازك: `dotnet user-secrets set "Authoring:Claude:ApiKey" "..." --project api/src/Wordrise.Host`. وفي بيئة التطوير المزوّد افتراضياً `Sample` (دروس تجريبية بلا تكلفة)، فلا يحتاج مفتاحاً.

| المفتاح | الافتراضي | المعنى |
|---|---|---|
| `Authoring:Provider` | `Claude` | `Sample` يكتب دروساً تجريبية بلا اتصال بأي خدمة |
| `Authoring:MaxCycles` | `3` | عدد محاولات الكتابة والمراجعة قبل أن يُعلَّم الدرس «يحتاج انتباهك» |
| `Authoring:Worker:Enabled` | `true` | `false` يوقف المعالجة الخلفية |
| `Authoring:Worker:PollIntervalSeconds` | `30` | كل كم ثانية يفحص الطابور |
| `Authoring:Worker:MaxDraftsPerPoll` | `3` | أقصى عدد مسودات في كل فحص |
| `Authoring:Claude:Writer` / `Reviewer` | Opus 5.5 (High) / Sonnet 5.5 (Medium) | النموذج والجهد وأقصى عدد رموز لكل دور |

إذا نقص المفتاح تفشل المسودة برسالة واضحة (والمتعلّمون لا يتأثرون)، وتعيدها بـ `requeue` بعد إضافته.

### تعيين المراجعين والتحقق بخطوتين

1. سجّل حساباً عادياً من الموقع بالبريد الذي ستستخدمه.
2. على السيرفر عيّن بريدك مديراً: متغير البيئة `Identity__AdminEmails__0=you@example.com` ثم أعد تشغيل التطبيق (يمنح دور المدير لحساب موجود فقط، ويمكن حذف الإعداد بعد ذلك).
3. سجّل الدخول من الموقع: ينقلك إلى `/admin/security` لتفعّل التحقق بخطوتين (أدخل المفتاح في تطبيق المصادقة، ثم الرمز، واحفظ رموز الاسترداد). ثم سجّل الخروج وادخل بالرمز.
4. من `/admin/staff` أضف المراجعين بالبريد (بعد أن يسجّلوا حساباتهم). كل مراجع يفعّل التحقق بخطوتين أيضاً.

بدون الرمز لا يفتح أي شيء من أدوات الدروس حتى لو كانت كلمة المرور صحيحة. وتغيير الأدوار يسري عند تسجيل الدخول التالي.

### الصوت (موديول `Media`)

عند نشر أي درس يسجّل النظام صوتاً لكل نص إنجليزي يسمعه المتعلّم (الكلمات، والأمثلة، وأسطر الحوار بصوتين مختلفين، والقاعدة، وخيارات المحادثة، وجملة الاستماع عادية وبطيئة) عبر Azure AI Speech، ويحفظه مرة واحدة في مجلد، ويقدّمه الـ API من `/api/v1/media/audio/{id}`. اسم التسجيل يُحسب من النص نفسه (`AudioId`)، فيجده الموقع بدون سؤال السيرفر (`core/audio-id.ts`).

- بدون إعداد المزوّد، أو إذا فشل تسجيل واحد: الدرس يُنشر عادياً، والموقع يقرأ بصوت الجهاز بدل التسجيل.
- للدروس المنشورة قبل ضبط Azure: `POST /api/v1/admin/lessons/audio` (للفريق) يسجّل الناقص فقط، ويمكن تكراره.

**مفتاح Azure لا يوضع في أي ملف:** `Media__Azure__Key` و`Media__Azure__Region` على السيرفر (أو user-secrets محلياً).

| المفتاح | الافتراضي | المعنى |
|---|---|---|
| `Media:Provider` | `None` | `Azure` لتفعيل التسجيل |
| `Media:Azure:Key` / `Region` | فارغ | من مورد Speech في Azure (مثلاً `westeurope`) |
| `Media:Azure:PrimaryVoice` | `en-US-JennyNeural` | صوت الكلمات وصوت المتعلّم في الحوار |
| `Media:Azure:SecondaryVoice` | `en-US-GuyNeural` | صوت الطرف الآخر في الحوار |
| `Media:Azure:SlowRate` | `-40%` | سرعة النسخة البطيئة في تمرين الاستماع |
| `Media:Storage:Path` | `media-data` | مجلد التسجيلات (لا يُرفع إلى git) |

### هيكل الـ API

- `src/Wordrise.Host`: المشروع الذي يُشغَّل. رفيع: المصادقة وCORS والصحة، ويسجّل الموديولات فقط في قائمة صريحة داخل `Program.cs`.
- `src/Shared`: الكلاس الأساسي `ModuleStartup` الذي يرث منه كل موديول، وإعدادات قاعدة البيانات المشتركة.
- `src/Modules/<Module>/*.Core`: كل موديول يملك بياناته (Schema خاص) والـ Controllers الخاصة به في مجلد `Controllers`، ونقطة دخوله `<Module>ModuleStartup`. الموديولات تبدأ حسب قيمة `Order` من الأصغر للأكبر.
- **موديول الهوية** (`Modules/Identity`): N-Layer، لأنه عمليات بسيطة.
- **موديول التعلّم** (`Modules/Learning`): Clean Architecture مع CQRS عبر مكتبة Mediator:
  - `Domain`: قواعد العمل فقط (الالتزام اليومي، والسلسلة، والتكرار المتباعد)، بدون أي اعتماد على قاعدة البيانات أو الويب.
  - `Application/Features/<المجال>/`: كل عملية في ملف واحد فيه الـ Command أو الـ Query مع الـ Handler.
  - `Infrastructure`: `Repositories` للكتابة، و`Queries` للقراءة، وقاعدة البيانات والـ Migrations.
  - `Api/Controllers`: Controllers رفيعة ترسل الـ Commands والـ Queries فقط.
- `src/Shared/Wordrise.Shared.Kernel`: نوع `Result` وقواعد العمل المشتركة، بدون أي اعتماديات.
### الربط مع الخدمات الخارجية (Claude، وAzure Speech، والبريد)

كل موديول يملك الربط الذي يحتاجه، حتى إذا فُصل يوماً إلى Microservice أخذ ما يحتاجه معه بدون تفكيك:

| النوع | المكان | مثال |
|---|---|---|
| ربط فيه منطق خاص بالموديول (الأغلب) | `Modules/<Module>/.../Infrastructure/Integrations/` | كتابة الدروس بـ Claude داخل موديول إنتاج المحتوى |
| عميل تقني عام تحتاجه عدة موديولات، بلا منطق أعمال | `src/Shared/Wordrise.Shared.Integrations.<Provider>`، ويصبح حزمة NuGet عند الفصل | عميل Azure Blob لرفع الملفات |
| خدمة يحتاجها الجميع | موديول مستقل تطلب منه البقية عبر الـ Contracts أو الأحداث | موديول Notifications لإرسال البريد |

وفي كل الحالات: الموديول لا يتصل بالخدمة الخارجية مباشرة من الـ Application، بل يعرّف واجهة يحتاجها وينفّذها الـ Infrastructure، فيمكن تغيير المزوّد بدون لمس منطق الموديول. والمفاتيح والعناوين من الإعدادات، لا من الكود.
- `src/Modules/<Module>/*.Contracts`: الشيء الوحيد الذي يُسمح لموديول آخر باستخدامه.
- `tests/Wordrise.ArchitectureTests`: تكسر البناء إذا استخدم موديولٌ داخلَ موديول آخر.

## أين يوجد ماذا في `web/src/app`

- `features/site/`: صفحة الموقع وصفحة التطبيق بملء الشاشة.
- `features/learner/`: تطبيق التعلّم. `learner.store.ts` يدير حالة الدرس، و`screens/` فيه الشاشات التسع. البيانات تأتي من `LearnerData` بتنفيذين: `ApiLearnerData` (الـ API الحقيقي، في `/app`) و`DemoLearnerData` (بيانات تجريبية في الذاكرة، للتجربة في الصفحة الرئيسية).
- `features/auth/`: صفحتا تسجيل الدخول وإنشاء الحساب.
- `core/auth/`: خدمة الدخول (`AuthService`) وحماية الصفحات (`authGuard`).
- `core/`: أيضاً النطق الصوتي (مؤقتاً من المتصفح) وتحويل الأرقام إلى العربية.
- `src/styles.css`: ألوان التصميم وخطوطه.
