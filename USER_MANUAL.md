## CareTrack (Apollo Learning) — User Manual

### Overview
CareTrack has **three main user roles**:
- **Apollo Admin**: platform owner (sets up universities, programmes, content, global governance).
- **University Admin**: manages a specific university tenant (students, cohorts, branding where allowed).
- **Student**: consumes learning content, completes assessments, downloads marksheets/certificates.

This manual explains:
- **What each role can do**
- **The full flow** from Apollo Admin → University Admin → Student
- **Common journeys** (content → learning → quiz → marksheet PDF)

---

## Roles & permissions (high level)

### Apollo Admin (Platform Owner)
Apollo Admin typically does platform-wide setup and administration:
- **University management**
  - Create partner universities
  - Update university details
  - Upload university assets (logo) where applicable
- **Programme management**
  - Create programmes
  - Create programme years/semesters/modules structure
- **Content management**
  - Create/update learning content (modules, lessons)
  - Publish modules/lessons to make them available
  - Maintain versions (content revisions)
- **Assessment management**
  - Configure quizzes for modules (questions/options, pass %)
  - Enable/disable assessments
- **Enrolment operations (if permitted)**
  - Create/import students
  - Assign students to cohorts
- **Platform branding (Apollo)**
  - Configure Apollo platform branding/logo (used as default fallback)
- **Reports**
  - View platform-level reports (universities, content performance, etc.)
- **Certificates / templates**
  - Configure certificate template (platform/tenant)
  - Upload template assets (logo, signatures)

### University Admin (Tenant Admin)
University Admin manages the university’s operational data:
- **Student enrolment**
  - Create students manually
  - Import students via CSV/XLSX
  - Assign students to a cohort
  - Update student profile/cohort/status
- **Cohort management**
  - Create cohorts (intake)
  - View cohorts list
- **University branding (where enabled)**
  - Upload university logo (used in student UI and PDFs)
- **Certificates / template (tenant)**
  - Maintain certificate template for their university (if allowed)
  - Upload signature images for PDFs
- **Reports**
  - University student reports and export (Excel/PDF)

### Student (Learner)
Student is focused on learning and completion:
- **Dashboard**
  - View current year/semester status
  - Resume learning
  - View schedule/calendar items (if enabled)
- **Curriculum**
  - View modules, lessons, progress
  - Open lessons (video/PDF)
  - Mark lesson/module completion (as designed)
- **Assessments**
  - Take module quiz after completing lessons
  - Submit assessment attempt
  - See pass/fail + score
- **Documents**
  - Download **Marksheet PDF** after a submitted attempt (latest attempt)
  - View certificates list (if issued)

---

## End-to-end flow (Apollo Admin → Student)

### 1) Apollo Admin — Platform & academic setup
Typical setup order:
- **Step 1: Create a University**
  - Add partner university record (name, domain, logo as required).
- **Step 2: Create Programme(s)**
  - Create a programme (ex: “Anaesthesia & OTT Technology”).
  - Create **Years** → **Semesters** → **Modules**.
- **Step 3: Attach content to modules**
  - Add lessons to modules (video/PDF assets).
  - Publish module when ready.
- **Step 4: Create/enable assessments**
  - Configure quiz for each module:
    - Title
    - Pass percentage
    - Attempts
    - Questions & answer options
  - Enable quiz to make it available to students.
- **Step 5 (Optional): Configure certificate template**
  - Set organization name, colors, logo/signatures, and text.
  - This is reused by PDF generation (certificate/marksheet style).

### 2) University Admin — Intake & student onboarding
Once programme and content exist:
- **Step 1: Create Cohort (intake)**
  - Create a cohort for the programme (ex: “2026 Intake — Apollo”).
  - Set intake year, starting year/semester.
- **Step 2: Add students**
  - Create individual students, or import from CSV/XLSX.
- **Step 3: Assign students to cohort**
  - Student must be linked to the cohort (programme path).
- **Step 4 (Optional): Upload university branding**
  - Upload university logo so student UI and documents display correct branding.

### 3) Student — Learning to completion
Student journey:
- **Step 1: Login**
  - Student authenticates and enters student dashboard.
- **Step 2: Learn lessons**
  - Student opens module lessons (video/PDF).
  - Progress is tracked; lessons/modules become completed.
- **Step 3: Take quiz**
  - Once module is complete, student attempts quiz.
  - Attempt is saved with score + passed flag.
- **Step 4: Download marksheet**
  - Student downloads **Marksheet PDF** for the quiz (latest submitted attempt).
- **Step 5: Certificate (optional)**
  - When a programme/criteria is completed, certificates may be issued.
  - Student can view certificates and download PDF if available.

---

## Key screens & what they mean

### Student: Curriculum → Module → Lesson
- **Lesson player**
  - Shows video or PDF content
  - Allows marking completion (as configured)
- **Module completion**
  - After all lessons are complete, quiz becomes available

### Student: Assessment result
After quiz submission:
- **Passed / Not passed**
- **Score percent**
- **Marksheet actions**
  - **View marksheet** (preview UI)
  - **Download marksheet (PDF)** (downloads a real PDF file)

---

## Marksheet PDF (Student)

### When student can download it
- Student must have a **submitted quiz attempt**.
- Download is for **latest submitted attempt** for that quiz.

### What the PDF contains
- University/organization header (logo + name)
- Student name
- Programme name
- Assessment title
- Score + pass mark
- Result (Pass/Attempted)
- Optional reference number (if certificate number exists)
- Signature blocks (if template signatures are configured)

---

## Troubleshooting & common issues

### 404 in Production for API routes
If production is hosted behind IIS, ensure frontend calls `/api/...` and backend supports the same base path.
This repo is configured to support **non-versioned API routes** under:
- `/api/<resource>/<action>`

### CORS errors after deploy
If frontend and backend are on different domains:
- Ensure CORS policy allows the frontend origin
- Ensure preflight (OPTIONS) is not blocked by reverse proxy/IIS rules

### Marksheet download works locally but not production
Common causes:
- Backend not redeployed with latest marksheet endpoint
- Reverse proxy base path mismatch (ensure `/api/...` routes are forwarded)
- Authentication header not reaching backend (proxy stripping headers)

---

## Recommended operational checklist (Deployment)

### Backend
- Set correct **ConnectionStrings**, **JWT settings**, and any external keys (as environment variables recommended)
- Confirm API reachable:
  - `/swagger` (if enabled)
  - `/api/auth/login`

### Frontend
- Build produces `web/caretrack-web/dist/`
- Ensure frontend base is configured to call `/api` (same domain deployment preferred)

---

## Appendix: Role-wise flow summary (quick)

### Apollo Admin
- Setup Universities → Programmes → Content → Quizzes → (Templates)

### University Admin
- Create Cohorts → Create/Import Students → Assign Cohorts → Branding/Signatures

### Student
- Learn lessons → Take quiz → Download marksheet PDF → View certificates

