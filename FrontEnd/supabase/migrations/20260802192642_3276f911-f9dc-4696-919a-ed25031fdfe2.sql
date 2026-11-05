
CREATE TABLE public.profiles (
  id uuid PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  email text,
  display_name text,
  created_at timestamptz NOT NULL DEFAULT now()
);
GRANT SELECT, INSERT, UPDATE ON public.profiles TO authenticated;
GRANT ALL ON public.profiles TO service_role;
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
CREATE POLICY "profiles_select_authenticated" ON public.profiles FOR SELECT TO authenticated USING (true);
CREATE POLICY "profiles_insert_own" ON public.profiles FOR INSERT TO authenticated WITH CHECK (auth.uid() = id);
CREATE POLICY "profiles_update_own" ON public.profiles FOR UPDATE TO authenticated USING (auth.uid() = id) WITH CHECK (auth.uid() = id);

CREATE TABLE public.lists (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id uuid NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  name text NOT NULL,
  color text NOT NULL DEFAULT 'blue',
  created_at timestamptz NOT NULL DEFAULT now()
);
GRANT SELECT, INSERT, UPDATE, DELETE ON public.lists TO authenticated;
GRANT ALL ON public.lists TO service_role;
ALTER TABLE public.lists ENABLE ROW LEVEL SECURITY;

CREATE TABLE public.list_shares (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  list_id uuid NOT NULL REFERENCES public.lists(id) ON DELETE CASCADE,
  user_id uuid NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  created_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (list_id, user_id)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON public.list_shares TO authenticated;
GRANT ALL ON public.list_shares TO service_role;
ALTER TABLE public.list_shares ENABLE ROW LEVEL SECURITY;

CREATE TABLE public.tasks (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  list_id uuid NOT NULL REFERENCES public.lists(id) ON DELETE CASCADE,
  created_by uuid NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  assigned_to uuid REFERENCES auth.users(id) ON DELETE SET NULL,
  title text NOT NULL,
  notes text,
  is_completed boolean NOT NULL DEFAULT false,
  is_important boolean NOT NULL DEFAULT false,
  is_my_day boolean NOT NULL DEFAULT false,
  due_date date,
  completed_at timestamptz,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);
GRANT SELECT, INSERT, UPDATE, DELETE ON public.tasks TO authenticated;
GRANT ALL ON public.tasks TO service_role;
ALTER TABLE public.tasks ENABLE ROW LEVEL SECURITY;

CREATE OR REPLACE FUNCTION public.has_list_access(_list_id uuid, _user_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
  SELECT EXISTS (SELECT 1 FROM public.lists l WHERE l.id = _list_id AND l.owner_id = _user_id)
      OR EXISTS (SELECT 1 FROM public.list_shares s WHERE s.list_id = _list_id AND s.user_id = _user_id)
$$;

CREATE OR REPLACE FUNCTION public.is_list_owner(_list_id uuid, _user_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
  SELECT EXISTS (SELECT 1 FROM public.lists l WHERE l.id = _list_id AND l.owner_id = _user_id)
$$;

CREATE POLICY "lists_select_access" ON public.lists FOR SELECT TO authenticated
  USING (owner_id = auth.uid() OR public.has_list_access(id, auth.uid()));
CREATE POLICY "lists_insert_own" ON public.lists FOR INSERT TO authenticated WITH CHECK (owner_id = auth.uid());
CREATE POLICY "lists_update_access" ON public.lists FOR UPDATE TO authenticated
  USING (owner_id = auth.uid() OR public.has_list_access(id, auth.uid()))
  WITH CHECK (owner_id = auth.uid() OR public.has_list_access(id, auth.uid()));
CREATE POLICY "lists_delete_owner" ON public.lists FOR DELETE TO authenticated USING (owner_id = auth.uid());

CREATE POLICY "list_shares_select" ON public.list_shares FOR SELECT TO authenticated
  USING (user_id = auth.uid() OR public.is_list_owner(list_id, auth.uid()));
CREATE POLICY "list_shares_insert_owner" ON public.list_shares FOR INSERT TO authenticated
  WITH CHECK (public.is_list_owner(list_id, auth.uid()));
CREATE POLICY "list_shares_delete" ON public.list_shares FOR DELETE TO authenticated
  USING (user_id = auth.uid() OR public.is_list_owner(list_id, auth.uid()));

CREATE POLICY "tasks_select_access" ON public.tasks FOR SELECT TO authenticated
  USING (public.has_list_access(list_id, auth.uid()) OR assigned_to = auth.uid());
CREATE POLICY "tasks_insert_access" ON public.tasks FOR INSERT TO authenticated
  WITH CHECK (created_by = auth.uid() AND public.has_list_access(list_id, auth.uid()));
CREATE POLICY "tasks_update_access" ON public.tasks FOR UPDATE TO authenticated
  USING (public.has_list_access(list_id, auth.uid()) OR assigned_to = auth.uid())
  WITH CHECK (public.has_list_access(list_id, auth.uid()) OR assigned_to = auth.uid());
CREATE POLICY "tasks_delete_access" ON public.tasks FOR DELETE TO authenticated
  USING (public.has_list_access(list_id, auth.uid()));

CREATE OR REPLACE FUNCTION public.touch_updated_at()
RETURNS trigger LANGUAGE plpgsql SET search_path = public AS $$
BEGIN NEW.updated_at = now(); RETURN NEW; END; $$;
CREATE TRIGGER tasks_touch_updated_at BEFORE UPDATE ON public.tasks
FOR EACH ROW EXECUTE FUNCTION public.touch_updated_at();

CREATE INDEX tasks_list_id_idx ON public.tasks(list_id);
CREATE INDEX tasks_assigned_to_idx ON public.tasks(assigned_to);
CREATE INDEX list_shares_user_id_idx ON public.list_shares(user_id);
