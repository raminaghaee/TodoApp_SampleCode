<!-- LOVABLE:BEGIN -->
> [!IMPORTANT]
> This project is connected to [Lovable](https://lovable.dev). Avoid rewriting
> published git history — force pushing, or rebasing/amending/squashing commits
> that are already pushed — as it rewrites history on Lovable's side and the
> user will likely lose their project history.
>
> Commits you push to the connected branch sync back to Lovable and show up in
> the editor, so keep the branch in a working state.
<!-- LOVABLE:END -->

- Keep demo data behind an explicit VITE_USE_MOCK_DATA opt-in and dynamically load its browser adapter only in demo mode; real API requests must never consult demo storage.
- Isolate demo session keys from real session keys so switching modes cannot send demo credentials to the backend.
- Shared switches must mirror thumb movement in RTL layouts so the thumb stays inside its track in both text directions.
- Use the shared apiUrl builder for every dedicated-backend request, including file downloads, so API versioning is centralized.
- List-sharing lookup consumes an array through searchProfiles; multi-selection submits the existing per-user share endpoint and retains failed selections for retry.
- Data mode resolves: saved browser choice (Home settings button) → VITE_USE_MOCK_DATA → Lovable-hosted domains default to demo; mode is fixed per page load, switching reloads.
- Task access uses the shared task-access policy with task, current user and parent list; deny missing context, guard task/child mutation handlers before API calls, and explicitly disable portal-based selectors. Shared error helpers preserve backend denials; the dedicated backend remains authoritative.
- Account pages reuse AccountForm and Toolbar; account requests use the dedicated cookie-auth API in account.ts, and profile updates never mutate email directly because email changes require a separate confirmation flow.
- Password recovery and email confirmation are public client pages; consume one-time tokens from the URL fragment, remove them from the address bar, and submit them only to the dedicated API. Demo credentials and tokens remain confined to demo storage and never represent email delivery.
- The open task is URL state on /tasks (a `task` search param), not component state, so notification clicks and shared links land on the task; revealing it shows its own list with search/filters cleared on the computed page, and an unknown id reports a message instead of a dead panel.
- Every delete action awaits the shared confirmDelete() dialog (ConfirmDialogHost mounted in __root) before calling the API; never use window.confirm. Why: one consistent warning before destructive actions.
