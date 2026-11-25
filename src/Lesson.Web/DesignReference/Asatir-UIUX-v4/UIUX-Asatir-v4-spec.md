mkdir -p /home/user/v4/doc && cd /home/user/v4/doc && cat > pages.md <<'MDEOF'
# UI/UX KIT v4 — اساطیر آینده | سند صفحه‌به‌صفحه برای پیاده‌سازی

> نسخهٔ ۴ · مکمل سند v3 · مبنا: `Lesson.sln` (.NET 9 / Blazor Server) + `wwwroot/site/index.html` (SPA هش‌دار)
> این سند «هر صفحه = یک بخش» است: Layout → Components → Data → Behaviors → فرم ورودی ادمین.

---

## ۰. قاعدهٔ حاکم: یک صفحه، چند دپارتمان

سه دپارتمان (زبان انگلیسی، موسیقی، کامپیوتر) و دپارتمان چهارم (نقاشی) **صفحهٔ جداگانه ندارند**.
همه از یک کامپوننت با پارامتر `Department` رندر می‌شوند.

```
Department enum: English | Music | Computer | Painting
Route pattern:
  #/{dept}/              → DeptHome      (مثل 38-dept-home.html)
  #/{dept}/courses       → CourseList    (30-courses-list.html)
  #/{dept}/courses/{id}  → CourseDetail  (31-course-detail.html)
  #/{dept}/videos        → VideoGrid     (33-videos-grid.html)
  #/{dept}/videos/{id}   → VideoWatch    (37-video-watch.html)
  #/{dept}/{media}       → LibraryList   (39-podcasts.html / 32-music-list.html / 35-stories.html)
  #/{dept}/books         → BooksZigzag   (34-books.html)
```

| پارامتر | اثر روی UI |
|---|---|
| `DeptAccent` | رنگ هیرو، رنگ حلقه‌ها، رنگ بج سطح (English=navy، Music=violet، Computer=teal، Painting=sand) |
| `Levelset` | زبان: A1..C1/IELTS · موسیقی: Grade 1..8 · کامپیوتر: Beginner/Intermediate/Advanced |
| `MediaKinds` | زبان: podcast/video/story/music/book · موسیقی: track/video/method · کامپیوتر: video/project/book |

**ممنوع:** کپی‌کردن صفحه برای هر دپارتمان. هر سه‌گانه‌سازی (triplication) در بازبینی رد می‌شود.

---

## ۱. توکن‌ها (خلاصهٔ اجرایی)

```
--navy-900:#06203C  --navy-800:#0A2B51  --navy-100:#DCE4EE  --navy-50:#F3F6FA
--sand-900:#4A3517  --sand-500:#D1AB7B  --sand-300:#E3CFA9  --sand-100:#F5E9D6  --sand-50:#FBF7EE
--cream:#F9F6ED    --paper:#FFFFFF      --ink:#111E2B       --ink-soft:#3B4E63   --muted:#647587
--jade:#0F7A57     --amber:#A16207      --coral:#C0392B     --violet:#5B4B8A
--line:#E4EAF1
--r-sm:8  --r-md:12  --r-lg:16  --r-xl:20  --r-2xl:26  --r-pill:999
--sh-1:0 1px 2px rgba(10,43,81,.06)          --sh-2:0 6px 16px -10px rgba(10,43,81,.24)
--sh-3:0 18px 34px -22px rgba(10,43,81,.4)   --sh-4:0 30px 60px -30px rgba(10,43,81,.5)
--ease:cubic-bezier(.22,.61,.36,1)  --spring:cubic-bezier(.34,1.4,.5,1)
--sidebar:264px  --topbar:68px  --maxw:1360px  --maxw-wide:1600px
```
تایپ: `Vazirmatn` (فارسی، لوکال `wwwroot/fonts/Vazirmatn-Variable.woff2`) + `Inter` (لاتین) + `Gulzar` فقط برای تیتر تزئینی.
مقیاس: 10.5 / 11.5 / 12.5 / 13.5 / 15 / 17 / 22 / 28 / clamp(22px,3vw,33px).
حرکت مجاز (فقط ۴ مورد): `fade-up 14px/240ms`، `drawer 420ms`، `ring-fill 500ms`، `stagger 40ms`. همه داخل `@media (prefers-reduced-motion: reduce)` خاموش.

---

## ۲. فهرست صفحات (Page Inventory)

| # | Page ID | Route | هدف | کامپوننت‌های اصلی | حالت‌ها |
|---|---|---|---|---|---|
| 01 | `site.home` | `#/` | صفحهٔ اصلی سایت | hero, carousel, dept-cards, courses-rail, stats | loading / empty-rail |
| 02 | `dept.home` | `#/{dept}` | صفحهٔ دپارتمان (پارامتری) | hero(dept), stations(5), courses-rail, latest-tracks, cta | loading / no-content |
| 03 | `courses.list` | `#/{dept}/courses` | فهرست دوره‌ها + فیلتر | dept-tabs, fchips(level/mode), course-card, sort | loading / empty / filter-empty |
| 04 | `courses.detail` | `#/{dept}/courses/{id}` | جزئیات دوره | story-strip, desc, schedule-table, members, topics, sample-video, satisfaction, buycard | loading / sold-out / not-found |
| 05 | `videos.grid` | `#/{dept}/videos` | گرید شبیه یوتیوب | yt-card, hover-preview, duration, level-badge, watched-bar | loading / empty / hover |
| 06 | `videos.watch` | `#/{dept}/videos/{id}` | صفحهٔ تماشا | player, meta, like/save, instructor, transcript, related | loading / restricted / ended |
| 07 | `podcasts.list` | `#/{dept}/{podcast}` | پادکست — دو حالت | pod-row, playbtn, txtbtn(MODE B only), inline-bar | playing / text-drawer / no-text |
| 08 | `music.list` | `#/{dept}/musics` | موسیقی — کشو مشروط | pod-row(music), txtbtn, drawer | playing+drawer / playing-only |
| 09 | `stories.list` | `#/{dept}/stories` | استوری‌های دایره‌ای | avatar-ring, filters, mini-preview | with-text / voice-only / seen |
| 10 | `stories.detail` | `#/{dept}/stories/{id}` | جزئیات استوری متن‌دار | banner, voice-player, justified-text, vocab, next | loading / no-text(redirect inline) |
| 11 | `books.list` | `#/{dept}/books` | کتاب‌ها — زیگزاگ | book-row, cover, price, collections | loading / empty / mobile-1col |
| 12 | `platform.student.dash` | `/student` | داشبورد دانش‌آموز | stat×4, continue-watch, next-session, skill-bars, quiz-table | loading / no-class |
| 13 | `platform.student.watch` | `/student/videos/{id}` | تماشای جلسه | player, progress-save, exercise, drawer(transcript) | restricted / done |
| 14 | `platform.teacher.dash` | `/teacher` | داشبورد مدرس | stat×4, student-table, today-tasks, content-mix | no-class(empty-state) |
| 15 | `platform.teacher.upload` | `/teacher/videos/new` | آپلود ۳ مرحله‌ای | stepper, template-picker, dropzone, mode-toggle, access | step1/2/3 / upload-error |
| 16 | `admin.dash` | `/admin` | داشبورد مدیر | stat×4, satisfaction-chart, role-donut, pending-coral, audit-rail | loading |
| 17 | `admin.templates` | `/admin/templates` | گالری قالب آماده | template-card×12, search | empty-search |
| 18 | `admin.library.form` | `/admin/site-library/new` | فرم رسانه (دو حالت) | modebox, dropzone, text/translation, relations, live-preview | MODE A / MODE B / validation |
| 19 | `admin.course.form` | `/admin/site-courses/{id}` | فرم دوره | basics, story-upload×6, schedule-repeater, sample, satisfaction | publish / draft / conflict |
| 20 | `admin.candidates` | `/admin/candidates` | داوطلبان تعیین سطح | stat×4, funnel, level-dist, table | ۴ status |
| 21 | `admin.users` | `/admin/users` | کاربران + دسترسی | table, role-matrix, devices | active/inactive |
| 22 | `mobile` | — | سه نمای موبایل | m-nav, m-card, full-drawer, story-detail | ≤767px |

---

## ۳. صفحه ۰۲ — دپارتمان (`dept.home`) — فایل `38-dept-home.html`

**Layout:** هیرو ۲۴۰–۳۰۰px با گرادیان `deptAccent` + halo شعاعی؛ سپس «ایستگاه‌ها» گرید auto-fill ۱۸۰px؛ سپس دو ستون: `courses-rail` (راست) و `latest-tracks` (چپ)؛ در ≤۹۰۰px یک ستون.

**Components:** `.depthero` (eyebrow + h1 + p + 3 CTA + 4 عدد آماری) · `.stgrid .stn` (ایموجی + عنوان + شمارش) · `.ccard` (۲ دورهٔ نزدیک) · `.plist .pod` (۲ قطعهٔ آخر).

**Data (`DepartmentSettings`):** `Slug`, `Title`, `Subtitle`, `HeroGradient`, `AccentHex`, `Stations[]`, `Stats{courses,tracks,videos,satisfaction}`.

**Behaviors:** کلیک ایستگاه → فهرست مربوط؛ شمارش‌ها از API (بدون هاردکد)؛ اگر `Stations` خالی → پیام «این دپارتمان در حال راه‌اندازی است».

**فرم ادمین:** `/admin/departments/{slug}` — عنوان، زیرعنوان، گرادیان (دو رنگ)، ایموجی هر ایستگاه، ترتیب ایستگاه‌ها (drag).

---

## ۴. صفحه ۰۳ — فهرست دوره‌ها (`courses.list`) — فایل `30-courses-list.html`

**Layout:** breadcrumb → `phead` (عنوان + جست‌وجو + select مرتب‌سازی) → `deptabs` (انگلیسی/موسیقی/کامپیوتر/نقاشی با شمارش) → دو ردیف `fchips` (سطح، نوع برگزاری) → `cgrid` سه‌ستونی.

**Components:** `.ccard` = قاب `ccard__media` (گرادیان دپارتمان + `glyph` = سطح + بج سطح + چیپ وضعیت) + بدنه (عنوان، جلسه/دقیقه/روز-ساعت، تاریخ شروع، مدرس، ستاره + تعداد نظر، «۱۲ از ۱۶ نفر»، قیمت + فلش).
زیر گرید: کارت قاعدهٔ پیاده‌سازی + دکمهٔ بازگشت.

**Data (`Course`):** `Id, Title, Department, Level, Mode, Sessions, Minutes, Weekdays[], Time, StartsAt, EndsAt, TeacherName, Price, Installments, Capacity, Enrolled, RatingAvg, RatingCount, Status(Open|Few|Full|Archived)`.

**States:** loading = ۳ اسکلتون هم‌اندازهٔ کارت؛ empty = «هنوز دوره‌ای برای این دپارتمان ثبت نشده است.» + دکمهٔ «دوره‌های دپارتمان دیگر»؛ filter-empty = «با این فیلترها دوره‌ای نیست» + «پاک کردن فیلترها»؛ Full = بج `chip--coral` «ظرفیت تکمیل» و دکمهٔ غیرفعال.

**Behaviors:** فیلتر/مرتب در URL هش (`#/english/courses?level=B1&mode=online&sort=rating`) تا اشتراک‌پذیر باشد؛ debounce جست‌وجو ۲۰۰ms؛ کلیک کارت → `courses.detail`.

**فرم ادمین:** همان فرم دوره (بخش ۷).

---

## ۵. صفحه ۰۴ — جزئیات دوره (`courses.detail`) — فایل `31-course-detail.html`

ترتیب عمودی ثابت (مهم برای آموزش کاربر):
۱. **story-strip** — نوار افقی دایره‌های ویدیوی دوره، بالای همه‌چیز.
۲. سرصفحهٔ دوره (بج‌ها، h1، ریتینگ/مدرس/جلسات).
۳. **توضیحات دوره** (متن + ۳ موضوع کلیدی).
۴. **برنامهٔ برگزاری** — جدول: جلسه | روز | تاریخ | ساعت | محل/لینک | موضوع.
۵. دو ستون: **اعضای دوره** (آواتار انباشته + نوار ظرفیت + میانگین سن/سطح/نسبت) و **چه بخش‌هایی تدریس می‌شود** (۶ آیتم شماره‌دار).
۶. **نمونه تدریس** (پلیر ۱۶:۹ + بج مدت + «بدون هزینه، بدون ثبت‌نام»).
۷. **رضایت محصلان** — امتیاز کل + ۵ محور `satbar` + دو نظر.
۸. `buycard` چسبان (sticky top:74px) — قیمت، اقساط، ۴ مزیت با تیک، CTA طلایی، مشاوره، کارت مدرس.

**Story-strip specs:** دایره ۶۸px، حلقهٔ گرادیان ۲px با `ring-fill` روی هاور، بج مدت در پایین دایره، `seen` = حلقه خاکستری + متن «دیده‌شده». کلیک → لایت‌باکس همان صفحه، `aspect-ratio:9/16` روی موبایل، نوار پیشرفت بالای لایت‌باکس (یک سگمنت به‌ازای هر ویدیو)، جابه‌جایی با ← →، بستن با ESC، `aria-label="ویدیوی دوره n"`.

**States:** sold-out (CTA غیرفعال + «افزودن به لیست انتظار»)، archived (بج خاکستری + «دورهٔ پایان‌یافته»)، not-found (کارت ۴۲۰px + «دوره پیدا نشد» + بازگشت).

**Data اضافه:** `Videos[]{id, thumb, duration, title, seen}`, `Schedule[]{no, weekday, date, time, place, topic}`, `Topics[]`, `Members{count, capacity, avgAge, entryLevel, ratio}`, `SampleVideoUrl`, `Satisfaction{overall, count, axes[]{label,value}}`, `Reviews[]{name, stars, date, text}`.

**فرم ادمین:** بخش ۱۹ (`admin.course.form`) — شش اسلات آپلود استوری، repeater برنامه با «تولید خودکار از الگوی هفتگی»، سوییچ نمایش رضایت.

**موبایل:** story-strip به اسکرول افقی با snap؛ جدول → کارت‌های جلسه (روز/تاریخ/ساعت در سه خط)؛ `buycard` به نوار پایین چسبیده با CTA تمام‌عرض.

---

## ۶. صفحه ۰۵ — گرید ویدیو (`videos.grid`) — فایل `33-videos-grid.html`

**Layout:** `ytgrid` = `repeat(auto-fill, minmax(268px,1fr))`، گپ ۱۸px ۱۴px.

**Card:** قاب `16/9` + گرادیان navy + دکمهٔ پخش مرکزی (ظهور در هاور، `opacity 0→1`, `translateY 4px`) + `ytcard__dur` پایین-چپ با `mono` + `ytcard__lvl` بالا-راست + نوار پیشرفت تماشا در لبهٔ پایین. زیر قاب: آواتار مدرس (۳۶px دایره) + عنوان دو خط با `line-clamp:2` + «مدرس · بازدید · زمان انتشار».

**Behaviors:** hover ≥۲ ثانیه → پیش‌نمایش ۱۰ ثانیه‌ای بی‌صدا (`<video muted playsinline>`) در همان قاب؛ خروج ماوس → توقف و بازگشت به قاب. کلیک کارت → `videos.watch` در صفحهٔ جدا (الزام کاربر). `loading="lazy"` روی تامبنیل‌ها.

**States:** watched (نوار کامل + تیک)، new (بج «جدید» ۷ روز)، restricted (قفل + «نیازمند ورود»)، empty («هنوز فیلمی آپلود نشده»).

**Data:** `Video{id, title, teacherName, teacherAvatar, level, duration, views, publishedAt, thumbnailUrl, previewUrl, progressPct}`.

---

## ۷. صفحه ۰۶ — تماشا (`videos.watch`) — فایل `37-video-watch.html`

**Layout:** دو ستون `minmax(0,1fr) 336px`؛ ≤۱۰۲۴px یک ستون با «بعدی‌ها» زیر پلیر.

**Blocks:** پلیر (کنترل سفارشی: پخش، صدا، CC، سرعت، تمام‌صفحه) · h1 + «بازدید · تاریخ · سطح · دسته» + اکشن‌ها (👍 ۱۲۴، ذخیره، اشتراک) · کارت مدرس + «دنبال کردن» · «دربارهٔ این فیلم» + هشتگ‌ها · **Transcript** با دو دکمه (نمایش / دانلود .srt) و متن justify · ستون کنار: ۴ «بعدی» با تامبنیل ۱۲۰px + کارت «این فیلم بخشی از دوره است» با لینک دوره.

**Behaviors:** ذخیرهٔ موقعیت هر ۱۰ ثانیه در `localStorage['video:'+id]`؛ `controlsList="nodownload"`؛ Esc از تمام‌صفحه؛ لینک زمانی در transcript (کلیک روی خط = `currentTime`).

**States:** restricted (قفل + «برای تماشا وارد شوید یا در دوره ثبت‌نام کنید»)، ended (کارت «بعدی را ببین» با شمارش ۵ ثانیه)، error («ویدیو بارگذاری نشد — تلاش مجدد»).

---

## ۸. صفحه ۰۷/۰۸ — پادکست و موسیقی: **قاعدهٔ دو حالت**

| شرط داده | کلاس | نمایش دکمهٔ متن | رفتار کلیک |
|---|---|---|---|
| `IsVoiceOnly == true` | `.badge-mode.a` «فقط صدا» | **هرگز رندر نمی‌شود** | پخش درجا (`playAudio`)، حلقهٔ پیشرفت روی همان ردیف، هیچ کشویی باز نمی‌شود |
| `!IsVoiceOnly && BodyText` | `.badge-mode.b` «صدا + متن» | `.txtbtn` «📄 متن» | کشوی راست `.trx` باز می‌شود؛ پخش ادامه دارد |
| `!IsVoiceOnly && !BodyText` | مثل حالت A | هرگز | اجباری پخش درجا (اعتبارسنجی ذخیره در ادمین جلوی این حالت را می‌گیرد) |
| `AudioContentId == null` | — | — | آیتم در سایت رندر **نمی‌شود** |

**Row anatomy:** `.pod` = آرت ۵۶px (با `.ring` پیشرفت) + بدنه (عنوان + بج حالت + متا: سطح/مدت + خلاصه + `.prog`) + `.pod__act` (پخش + متن) + `.pstrip` (نوار موج ۱۲ ستون + زمان + ±۱۵ ثانیه) که فقط هنگام پخش رندر می‌شود.

**Drawer `.trx` — مشخصات قطعی:**
- همیشه از **راست فیزیکی** (`right:0`؛ تنها استثنای مجاز `right` در پروژه) تا در `dir=ltr` هم درست باز شود.
- عرض: ≥۱۰۲۴px → `440px` · ۷۶۸–۱۰۲۳px → `min(88vw,440px)` · <۷۶۸px → `100%` تمام‌صفحه.
- باز/بسته ۴۲۰ms با `requestAnimationFrame` + کلاس `.is-open`; بک‌دراپ `rgba(6,20,38,.5)`.
- **`audio.pause()` ممنوع** در باز/بستن. ESC فقط کشو را می‌بندد (`e.stopPropagation()`).
- فوکوس‌تراپ + بازگشت فوکوس به `.txtbtn`؛ قفل اسکرول بدنه با جبران `padding-inline-end` (جلوگیری از پرش RTL).
- تب‌ها: «متن اصلی» / «ترجمه فارسی»؛ اگر ترجمه خالی → «ترجمه‌ای موجود نیست.»؛ اگر متن خالی → «متنی برای این مورد ثبت نشده است.» (عملاً رخ نمی‌دهد).
- خط در حال پخش: `.sync` با پس‌زمینه `rgba(209,171,123,.22)` + `scrollIntoView({block:'center'})`.
- برچسب «📄 متن آهنگ» در موسیقی، «📄 متن» در پادکست — فقط لیبل تغییر می‌کند.

**یک پخش‌کننده در کل صفحه:** تابع واحد `playAudio(url, title, el)` اول `currentAudio.pause()` بعد `src` جدید. ذخیرهٔ موقعیت `localStorage['audio:'+id]` هر ۵ ثانیه.

**فرم ادمین:** `admin.library.form` با `modebox` دوگزینه‌ای + اعتبارسنجی سرور: MODE B با متن خالی → خطای «متن نمی‌تواند خالی باشد»; MODE A → `BodyText` پاک می‌شود.

---

## ۹. صفحه ۰۹/۱۰ — استوری‌ها — فایل‌های `35-stories.html` / `36-story-detail.html`

**List:** `.storygrid` = `auto-fill minmax(112px,1fr)`؛ هر آیتم: حلقهٔ ۷۲px (گرادیان sand برای متن‌دار، خاکستری برای `seen`، نشان 🎧 برای فقط صدا) + عنوان یک‌خطی + «A2 · ۴:۱۰». فیلترها: همه / با متن / فقط صدا / سطح.

**Branch:**
- `HasText` → ناوبری به `#/{dept}/stories/{id}`.
- `!HasText` → پخش درجا، حلقه به «حال پخش» تبدیل می‌شود (پالس ۱.۶s)، بدون ناوبری.

**Detail (ترتیب اجباری):** ۱) **بنر** بالای صفحه `aspect-ratio:16/6` (موبایل ۱۶:۹) با عنوان بزرگ فانتزی کم‌کنتراست + بج سطح/مدت. ۲) h1 + متای «سطح · تعداد واژه · نویسنده · تاریخ». ۳) **voice player** (دکمهٔ ۴۶px + موج + زمان جاری/کل + سرعت ۰.۷۵/۱/۱.۲۵ + «تکرار جمله»). ۴) **متن با `text-align:justify`**، `line-height:2.25`، پاراگراف نقل‌قول با پس‌زمینهٔ `#FBF3DE` و `border-inline-start:3px`. ۵) سه کارت پایین: واژگان کلیدی، داستان بعدی، دانلود PDF/پادکست/آزمون.

**States:** loading = اسکلتون بنر+دو خط؛ error = «داستان پیدا نشد»؛ `no-text` روی URL جزئیات → ریدایرکت به لیست + پخش خودکار همان آیتم.

**Data:** `Story{id, title, level, durationSec, wordCount, bannerUrl, audioUrl, bodyText, translation?, vocab[]{en,fa}, nextId}`.

---

## ۱۰. صفحه ۱۱ — کتاب‌ها (زیگزاگ) — فایل `34-books.html`

**Layout:** `.bookrow` = `grid-template-columns:290px 1fr`؛ آیتم زوج `.bookrow.rev` = `1fr 290px` با `order:2` روی تصویر. فاصلهٔ ۱۶px بین ردیف‌ها، هر ردیف کارت سفید با `r-xl`.

**Item:** جلد `aspect-ratio:3/4` با گرادیان اختصاصی + لوگوی ناشر + عنوان کتاب + ویرایش؛ سمت متن: h2، نویسنده · سال · صفحات؛ `.coll` = چیپ‌های مجموعه/سطح/ضمیمه؛ پاراگراف معرفی؛ ردیف پایانی: **قیمت** (bold ۱۷px + «تومان»)، وضعیت موجودی (`jade` موجود / `amber` «۲ نسخهٔ آخر» / خاکستری «ارسال ۲ روزه»)، دکمهٔ «مشاهده و خرید».

**Behavior:** کلیک ردیف → `#/{dept}/books/{id}` (صفحهٔ کتاب: جلد بزرگ، توضیح بلند، فهرست فصل‌ها، «افزودن به سبد»). موبایل ≤۸۶۰px: یک ستون، تصویر همیشه بالا، `order:0`.

**CSS لازم:** `.bookrow:nth-child(even) .bookrow__img{order:2}` — بدون کلاس دستی.

**Data:** `Book{id, title, author, year, pages, coverUrl, price, stock, collections[], summary, level}`.

---

## ۱۱. پلتفرم — دانش‌آموز (`40-student-dash.html`, `41-student-watch.html`)

**Shell:** `.plat` = `264px 1fr`؛ سایدبار گرادیان navy با گروه‌بندی «اصلی / پیشرفت / حساب»؛ توپ‌بار ۶۰px چسبان با جست‌وجو + آواتار. ≤۱۰۲۴px سایدبار → کشوی همبرگری.

**Dash blocks:** ۴ کارت آماری (پیشرفت دوره ۶۴٪ با کارت navy accent، ویدیوهای دیده‌شده، میانگین آزمون، زنجیرهٔ حضور) · «ادامهٔ تماشا» (پلیر با نوار ۶۲٪ + دکمهٔ ادامهٔ درست از همان ثانیه) · «جلسهٔ بعدی» (تاریخ/ساعت/محل + یادآور + مسیر) · رادار مهارت‌ها (۴ محور) · جدول آزمون‌ها با بج وضعیت.

**Watch page:** پلیر + «پیشرفت هر ۱۰ ثانیه ذخیره می‌شود» + تمرین بعد از تماشا (رادیو با حالت انتخاب‌شده navy) + دکمهٔ «📄 متن جلسه» که همان `.trx` را باز می‌کند (هم‌خوانی انگلیسی/ترجمه).

**States:** no-class → EmptyState «هنوز در دوره‌ای ثبت‌نام نکرده‌اید» + CTA فهرست دوره‌ها؛ restricted → «دسترسی به این جلسه ندارید»؛ quiz pending → بج «در انتظار تصحیح».

---

## ۱۲. پلتفرم — مدرس (`42-teacher-dash.html`, `47-teacher-upload.html`)

**Dash:** ۴ کارت (دانش‌آموزان فعال، محتوای منتشرشده، آزمون در جریان، میانگین نمرهٔ کلاس) · جدول دانش‌آموزان با نوار پیشرفت رنگی (jade ≥۸۰٪، navy ۵۰–۸۰، coral <۵۰) · «کارهای امروز» ۳ ردیف رنگی با CTA · ترکیب محتوا (نوار انباشته ۴ رنگ).
**Empty:** مدرس بدون کلاس → «کلاسی به شما اختصاص داده نشده» + «درخواست از مدیر».

**Upload (۳ مرحله):**
- قدم ۱ **انتخاب قالب** (`admin.templates`): پادکست فقط صدا / صدا+متن / موسیقی / داستان / ویدیو / کتاب.
- قدم ۲ **فایل و متن**: dropzone (mp3/m4a/wav ≤۲۰۰MB) → پس از آپلود: نوار موج + مدت خودکار؛ `modebox` «آیا متن دارد؟»؛ فیلد متن انگلیسی و ترجمه با راهنمای رفتار کشو.
- قدم ۳ **انتشار**: پوشه، کلاس، سوییچ «نمایش در سایت عمومی» و «محدود به کلاس».
**States:** upload-error («فرمت پشتیبانی نمی‌شود / حجم بیش از ۲۰۰MB»)، encoding («در حال استخراج مدت زمان…»)، draft-saved (toast).

---

## ۱۳. PanelAdmin — `43` تا `50`

| Page | فایل | نکتهٔ کلیدی طراحی |
|---|---|---|
| داشبورد | `43-admin-dash.html` | ۴ کارت + نمودار رضایت ۶ ماهه + دونات نقش‌ها + **کارت مرجانی «۴ داوطلب نیازمند تماس»** + جدول فعالیت با مهر زمانی شمسی |
| قالب‌های آماده | `49-admin-templates.html` | ۱۲ قالب؛ هر کارت: ایموجی، نام، «n فیلد»، دو خط توضیح رفتار؛ کارت انتخابی با حلقهٔ navy |
| فرم کتابخانه | `44-admin-library-form.html` | **modebox** دوگزینه‌ای + dropzone + متن/ترجمه + قبلی/بعدی/مرتبط + ترتیب + **پیش‌نمایش زندهٔ ردیف پادکست** + سوییچ‌های انتشار |
| فرم دوره | `45-admin-course-form.html` | ۴ بلوک: مشخصات (۱۲ فیلد) · شش اسلات استوری · repeater برنامه (`52px 1fr 96 96 110 34`) با «تولید خودکار از الگوی هفتگی» و «بررسی تعطیلات» · نمونه تدریس + رضایت (۵ محور، محاسبهٔ خودکار) |
| داوطلبان | `46-admin-candidates.html` | ۴ کارت + قیف ۴ مرحله‌ای + توزیع سطح + جدول با **نمودار میله‌ای مهارت‌ها V/L/R/S** و بج وضعیت (نیازمند/تماس‌گرفته/ثبت‌نام/رهاکرده) |
| کاربران | `50-admin-users.html` | جدول با آواتار حرفی، نقش، آخرین ورود، وضعیت + **ماتریس دسترسی نقش‌ها** + کارت دستگاه‌ها با «پایان نشست» |

**قواعد عمومی پنل:** توپ‌بار چسبان حاوی عنوان + بج زمینه + اکشن‌های اصلی (ذخیره/پیش‌نمایش/انصراف)؛ هر فرم در ستون محتوا + ستون کنار چسبان (وضعیت/انتشار/لینک به صفحهٔ عمومی)؛ جدول‌ها با `th` راست‌چین و ردیف هاور `#FAFBFD`؛ اعداد شمسی و `mono` برای تاریخ/ساعت/تلفن؛ هرگز `alert()` — خطا به‌صورت کارت خطا بالای فرم.

---

## ۱۴. موبایل (`48-mobile.html`)

سه نما: **۳۰** فهرست دوره‌ها (کارت تک‌ستونی، نوار دستهٔ افقی، قیمت برجسته، ریتینگ) · **۳۲** کشوی متن تمام‌صفحه (هدر navy، پلیر کوچک، متن اسکرول، بستن ۲۲px، «ESC/✕ = بستن · پخش ادامه دارد») · **۳۶** جزئیات استوری (بنر → پلیر → متن justify).

Breakpoints: 1180 (گرید ۳→۲)، 900 (سایدبار→کشو، دو ستون→یک)، 767 (کشو تمام‌صفحه، جدول→کارت)، 480 (فونت متن ۱۳.۵→۱۳، دایرهٔ استوری ۷۲→۶۴).

---

## ۱۵. پرامپت آماده برای ChatGPT (کپی عین)

```
Stack: .NET 9 / Blazor Server (Lesson.Web). Working dir: src/Lesson.Web
CSS موجود: wwwroot/css/ds/{tokens,base,components,layout,patterns}.css و wwwroot/site/css/site-index.css
فونت لوکال: wwwroot/fonts/Vazirmatn-Variable.woff2  (Google Fonts ممنوع)

هدف: پیاده‌سازی UI/UX kit v4 روی پروژهٔ موجود، بدون تغییر منطق بک‌اند.
1) tokens.css را با توکن‌های بخش ۱ جایگزین کن؛ aliasهای قدیمی (--color-primary, --shadow-card, --radius-*) را حفظ کن.
2) یک کامپوننت CoursePage(Department, CourseId) بساز که هم فهرست و هم جزئیات را پوشش دهد؛ برای Music/Computer دوباره نساز.
3) StoryStrip در بالای CourseDetail: حلقه‌های ۶۸px + لایت‌باکس ۹:۱۶ + نوار پیشرفت سگمنتی.
4) قاعدهٔ دو حالت رسانه را در سه جا اعمال کن (site index.html، کتابخانه، پلتفرم):
   if (IsVoiceOnly || !BodyText) → فقط playAudio(...) و هیچ drawer؛ else → دکمهٔ .txtbtn + کشوی .trx
5) کشو .trx را فرزند مستقیم <body> رندر کن (بیرون .app-shell)؛ right:0؛ عرض 440/88vw/100%؛ ESC فقط کشو؛ audio.pause ممنوع.
6) BooksZigzag: grid 290px/1fr با nth-child(even) order:2؛ زیر 860px یک ستون.
7) VideosGrid شبیه یوتیوب + مسیر جدا #/english/videos/{id} → VideoWatch با transcript و بعدی‌ها.
8) Stories: HasText → route detail (banner → player → justified text)؛ else پخش درجا.
9) PanelAdmin: صفحهٔ «قالب‌های آماده» + فرم‌های مبتنی بر قالب؛ modebox دوگزینه‌ای با اعتبارسنجی سرور.
10) RTL: منطقی بنویس (inset-inline-start/end, margin-inline, border-inline-start, text-align:start). justify فقط برای متن داستان.
11) اعداد فارسی: toLocaleString('fa-IR')؛ تاریخ‌ها شمسی.
12) Footer CSS: responsive.css آخر لود شود؛ @import ممنوع؛ فوکوس = var(--ring).
تحویل: tokens.css, components.css, صفحات فوق، و اسکرین‌شات تطبیق با mockups/30..50.
```

---

## ۱۶. چک‌لیست QA

- [ ] هر صفحهٔ فهرست بالا یک mockup + یک screenshot دارد.
- [ ] در هیچ صفحه‌ای برای آیتم «فقط صدا» دکمهٔ متن رندر نمی‌شود.
- [ ] کشو در `dir=ltr` هم از راست باز می‌شود و پخش را قطع نمی‌کند.
- [ ] زیگزاگ کتاب‌ها در ۸۶۰px به یک ستون می‌رسد و تصویر بالا می‌ماند.
- [ ] Course/Music/Video برای سه دپارتمان از یک کامپوننت رندر می‌شوند (بدون کد سه‌گانه).
- [ ] رضایت هر دوره در صفحهٔ دوره دیده می‌شود (کل + ۵ محور + تعداد نظر).
- [ ] جدول برنامهٔ هفتگی در موبایل به کارت تبدیل می‌شود.
- [ ] همهٔ CTAها حداقل ۴۴×۴۴px؛ همهٔ آیکون‌ها `aria-hidden` با `aria-label` روی دکمه.
- [ ] `prefers-reduced-motion` همهٔ انیمیشن‌ها را خاموش می‌کند.
MDEOF
wc -l pages.md && echo SPEC_OK