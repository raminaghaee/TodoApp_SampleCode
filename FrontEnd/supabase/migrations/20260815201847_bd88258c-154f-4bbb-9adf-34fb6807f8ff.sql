-- 1. Lock down helper function execution
REVOKE ALL ON FUNCTION public.has_role(uuid, public.app_role) FROM PUBLIC, anon;
REVOKE ALL ON FUNCTION public.has_list_access(uuid, uuid) FROM PUBLIC, anon;
REVOKE ALL ON FUNCTION public.is_list_owner(uuid, uuid) FROM PUBLIC, anon;
REVOKE ALL ON FUNCTION public.has_task_access(uuid, uuid) FROM PUBLIC, anon;
REVOKE ALL ON FUNCTION public.notify_task_assignment() FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.touch_updated_at() FROM PUBLIC, anon, authenticated;

-- 2. Notifications: no spoofing other users
DROP POLICY IF EXISTS notifications_insert_authenticated ON public.notifications;
CREATE POLICY notifications_insert_self ON public.notifications
  FOR INSERT TO authenticated
  WITH CHECK (user_id = auth.uid());

-- 3. Profiles: only own profile, collaborators, or admins
DROP POLICY IF EXISTS profiles_select_authenticated ON public.profiles;
CREATE POLICY profiles_select_related ON public.profiles
  FOR SELECT TO authenticated
  USING (
    id = auth.uid()
    OR public.has_role(auth.uid(), 'super_admin')
    OR EXISTS (
      SELECT 1 FROM public.lists l
      WHERE (l.owner_id = public.profiles.id AND public.has_list_access(l.id, auth.uid()))
         OR (l.owner_id = auth.uid() AND EXISTS (
              SELECT 1 FROM public.list_shares s
              WHERE s.list_id = l.id AND s.user_id = public.profiles.id))
    )
    OR EXISTS (
      SELECT 1 FROM public.tasks t
      WHERE (t.assigned_to = public.profiles.id OR t.created_by = public.profiles.id)
        AND (public.has_list_access(t.list_id, auth.uid()) OR t.assigned_to = auth.uid())
    )
  );