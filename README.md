# Wordrise

منصة خيرية مجانية لتعليم الإنجليزية للمتحدثين بالعربية.

## المجلدات

| المجلد | المحتوى |
|---|---|
| `web/` | الموقع: Angular 22 مع SSR ودعم الاتجاه من اليمين لليسار |
| `api/` | الـ Backend: ASP.NET Core (.NET 10) بمعمارية Modular Monolith |

## تشغيل الموقع

```bash
cd web
npm install
npm start
```

ثم افتح `http://localhost:4200`:

- `/`: الموقع، وفيه التطبيق التفاعلي مدمجاً.
- `/app`: التطبيق بملء الشاشة للجوال.

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
- `src/Integrations`: الربط مع الخدمات الخارجية (الذكاء الاصطناعي، والصوت، والبريد). القواعد في `src/Integrations/README.md`.
- `src/Modules/<Module>/*.Contracts`: الشيء الوحيد الذي يُسمح لموديول آخر باستخدامه.
- `tests/Wordrise.ArchitectureTests`: تكسر البناء إذا استخدم موديولٌ داخلَ موديول آخر.

## أين يوجد ماذا في `web/src/app`

- `features/site/`: صفحة الموقع وصفحة التطبيق بملء الشاشة.
- `features/learner/`: تطبيق التعلّم. `learner.store.ts` يدير حالة الدرس، و`lesson.data.ts` فيه بيانات درس تجريبي إلى أن يُربط الـ API، و`screens/` فيه الشاشات التسع.
- `core/`: النطق الصوتي (مؤقتاً من المتصفح) وتحويل الأرقام إلى العربية.
- `src/styles.css`: ألوان التصميم وخطوطه.
