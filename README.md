# LMS - Learning Management System

LMS is a comprehensive Learning Management System built with **ASP.NET Core MVC**. It allows educational institutions to manage courses, instructors, and students efficiently. The system features real-time communication, AI-powered quiz generation, live classes, and a robust role-based access control system.

## Key Features

- **Role-Based Access Control**: Distinct dashboards for Admins, Instructors, and Students.
- **Course Management**: Create, edit, and manage courses with rich multimedia.
- **AI-Powered Quizzes**: Leverage AI to automatically generate quizzes.
- **Real-Time Communication**: SignalR-based chat and live classes.
- **Grading & Analytics**: Track progress and automate grading.
- **Secure Authentication**: Built-in security for user accounts.

## Tech Stack

- **Framework**: ASP.NET Core 8 MVC
- **Database**: MySQL with Entity Framework Core
- **Real-time**: SignalR
- **Frontend**: Razor Views, Bootstrap, JavaScript
- **AI Integration**: AI Service for Quiz Generation

## How to Run

Follow these steps to set up and run the project locally:

1.  **Clone the Repository**

    ```bash
    git clone https://github.com/sameer9860/LMS.git
    ```

2.  **Navigate to the Project Folder**

    ```bash
    cd LMS
    ```

3.  **Configure Database**
    Update `appsettings.json` with your MySQL database credentials.

4.  **Setup Database Migrations**
    - **Remove existing migrations:** Delete the `Migrations` folder if it exists.
    - **Create initial migration:**
      ```bash
      dotnet ef migrations add InitialCreate
      ```
    - **Update database:**
      ```bash
      dotnet ef database update
      ```

5.  **Run the Application**
    ```bash
    dotnet run
    ```

---

## Gallery

### 1. Authentication

**Login Page**
![Login](ScreenShots/login.png)

---

### 2. Admin Module

_Complete system management control._

**Dashboard Overview**
![Admin Dashboard](ScreenShots/adminDashboard.png)
![Admin Dashboard 2](ScreenShots/adminDashboard2.png)
![Admin Dashboard 3](ScreenShots/adminDashboard3.png)

**Instructor Management Workflow**
_Viewing, Adding, and Deleting Instructors_
![Instructor List](ScreenShots/instructorList.png)
![Add Instructor Step 1](ScreenShots/addInstructor1.png)
![Add Instructor Step 2](ScreenShots/addInstructor2.png)
![Add Instructor Step 3](ScreenShots/addInstructor3.png)
![Success Message](ScreenShots/successInstructor.png)
![Confirm Delete](ScreenShots/confirmDeleteInstructor.png)

**Student Management Workflow**
_Viewing and Adding Students_
![Student List](ScreenShots/studentList.png)
![Create Student Step 1](ScreenShots/createStudent1.png)
![Create Student Step 2](ScreenShots/createStudent2.png)
![Create Student Step 3](ScreenShots/createStudent3.png)
![Create Student Step 4](ScreenShots/createStudent4.png)
![Success Message](ScreenShots/sucessStudent.png)

**Profile & Security**
![Admin Profile](ScreenShots/adminProfile.png)
![Change Password](ScreenShots/adminChangePassword.png)

---

### 3. Instructor Module

_Course creation, student assessment, and monitoring._

**Dashboard**
![Instructor Dashboard](ScreenShots/instructorDashboard.png)
![Instructor Dashboard 2](ScreenShots/instructorDashboard2.png)
![Instructor Dashboard 3](ScreenShots/instructorDashboard3.png)
![Instructor Dashboard 4](ScreenShots/instructorDashboard4.png)

**Course Management**
![My Courses](ScreenShots/myCourse.png)
![Create Course](ScreenShots/createCourse.png)
![Edit Course](ScreenShots/editCourse.png)
![Course Description](ScreenShots/courseDescription.png)
![Course Materials](ScreenShots/courseMaterials.png)
![Course Participants](ScreenShots/courseParticipants.png)

**Assessments (Assignments & Quizzes)**
![Assignment List](ScreenShots/courseAssignment.png)
![Assignment Details](ScreenShots/courseAssignment2.png)
![Auto Quiz Generation](ScreenShots/courseAutoQuiz.png)
![Manual Quiz Creation](ScreenShots/courseManualQUiz.png)
![Manual Quiz Description](ScreenShots/courseManualQuizDescription.png)

**Grading & Student Monitoring**
![Student Activity](ScreenShots/specificStudentActivity.png)
![View Submissions](ScreenShots/instructorViewSubmissions.png)
![Grading Interface](ScreenShots/instructorViewSubmissionsGrading.png)
![Graded Submission](ScreenShots/instructorViewSubmissionsGraded.png)
![Quiz Submissions](ScreenShots/instructorQuizSubmissions.png)
![Quiz Graded](ScreenShots/instructorQuizGraded.png)

**Communication**
![Course Chat](ScreenShots/courseChat.png)
![Live Class](ScreenShots/courseLiveClass.png)

**Profile & Security**
![Instructor Profile](ScreenShots/instructorProfile.png)
![Profile View](ScreenShots/istructoProfile.png)
![Change Password](ScreenShots/instructorChangePassword.png)

---

### 4. Student Module

_Learning environment, quiz taking, and progress tracking._

**Dashboard**
![Student Dashboard](ScreenShots/studentDashboard.png)
![Student Dashboard 2](ScreenShots/studentDashboard2.png)

**Course Experience**
![Course List](ScreenShots/studentCourseLists.png)
![Course Description](ScreenShots/studentCourseDescription.png)
![Course Materials](ScreenShots/studentCourrseMaterials.png)
![Course Chat](ScreenShots/studentCourseChat.png)
![Live Class](ScreenShots/studentCourseLiveClass.png)

**Assignments & Quizzes**
![Assignments](ScreenShots/studentCourseAssignments.png)
![Quizzes](ScreenShots/studentCourseQuizzes.png)
![Taking Quiz](ScreenShots/studentCourseDemoQuizTaking.png)
![Quiz Submission](ScreenShots/studentCourseDemoQuizSubmission.png)
![Quiz Result](ScreenShots/studentCourseDemoQuizResult.png)
![Assignment Scores](ScreenShots/studentCourseAssignmentQuizScores.png)

**Results & Profile**
![Results & Grades](ScreenShots/studentResultsAndGrade.png)
![Results Detailed](ScreenShots/studentResultsAndGrade2.png)
![Student Profile](ScreenShots/studentProfile.png)
![Profile Details](ScreenShots/studentProfileStudent.png)
![Change Password](ScreenShots/studentChangePassword.png)
