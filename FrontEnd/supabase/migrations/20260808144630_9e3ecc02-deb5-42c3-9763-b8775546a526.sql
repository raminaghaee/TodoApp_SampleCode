REVOKE ALL ON FUNCTION public.notify_task_assignment() FROM anon, authenticated, public;
REVOKE ALL ON FUNCTION public.touch_updated_at() FROM anon, authenticated, public;
REVOKE ALL ON FUNCTION public.has_role(uuid, public.app_role) FROM anon, public;
REVOKE ALL ON FUNCTION public.has_list_access(uuid, uuid) FROM anon, public;
REVOKE ALL ON FUNCTION public.is_list_owner(uuid, uuid) FROM anon, public;
GRANT EXECUTE ON FUNCTION public.has_role(uuid, public.app_role) TO authenticated;
GRANT EXECUTE ON FUNCTION public.has_list_access(uuid, uuid) TO authenticated;
GRANT EXECUTE ON FUNCTION public.is_list_owner(uuid, uuid) TO authenticated;