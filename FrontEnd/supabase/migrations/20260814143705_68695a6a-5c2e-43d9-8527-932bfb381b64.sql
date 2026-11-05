-- Subtasks
CREATE TABLE public.subtasks (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  task_id uuid NOT NULL REFERENCES public.tasks(id) ON DELETE CASCADE,
  title text NOT NULL,
  is_completed boolean NOT NULL DEFAULT false,
  position integer NOT NULL DEFAULT 0,
  created_by uuid NOT NULL REFERENCES auth.users(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

GRANT SELECT, INSERT, UPDATE, DELETE ON public.subtasks TO authenticated;
GRANT ALL ON public.subtasks TO service_role;
ALTER TABLE public.subtasks ENABLE ROW LEVEL SECURITY;

CREATE OR REPLACE FUNCTION public.has_task_access(_task_id uuid, _user_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE SECURITY DEFINER
SET search_path = public
AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.tasks t
    WHERE t.id = _task_id
      AND (public.has_list_access(t.list_id, _user_id) OR t.assigned_to = _user_id)
  )
$$;

REVOKE ALL ON FUNCTION public.has_task_access(uuid, uuid) FROM anon;
GRANT EXECUTE ON FUNCTION public.has_task_access(uuid, uuid) TO authenticated;

CREATE POLICY subtasks_select ON public.subtasks FOR SELECT TO authenticated
  USING (public.has_task_access(task_id, auth.uid()));
CREATE POLICY subtasks_insert ON public.subtasks FOR INSERT TO authenticated
  WITH CHECK (created_by = auth.uid() AND public.has_task_access(task_id, auth.uid()));
CREATE POLICY subtasks_update ON public.subtasks FOR UPDATE TO authenticated
  USING (public.has_task_access(task_id, auth.uid()))
  WITH CHECK (public.has_task_access(task_id, auth.uid()));
CREATE POLICY subtasks_delete ON public.subtasks FOR DELETE TO authenticated
  USING (public.has_task_access(task_id, auth.uid()));

CREATE TRIGGER subtasks_touch_updated_at BEFORE UPDATE ON public.subtasks
  FOR EACH ROW EXECUTE FUNCTION public.touch_updated_at();

CREATE INDEX subtasks_task_id_idx ON public.subtasks(task_id);

-- Attachments
CREATE TABLE public.task_attachments (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  task_id uuid NOT NULL REFERENCES public.tasks(id) ON DELETE CASCADE,
  storage_path text NOT NULL,
  file_name text NOT NULL,
  mime_type text,
  size_bytes bigint,
  uploaded_by uuid NOT NULL REFERENCES auth.users(id),
  created_at timestamptz NOT NULL DEFAULT now()
);

GRANT SELECT, INSERT, UPDATE, DELETE ON public.task_attachments TO authenticated;
GRANT ALL ON public.task_attachments TO service_role;
ALTER TABLE public.task_attachments ENABLE ROW LEVEL SECURITY;

CREATE POLICY task_attachments_select ON public.task_attachments FOR SELECT TO authenticated
  USING (public.has_task_access(task_id, auth.uid()));
CREATE POLICY task_attachments_insert ON public.task_attachments FOR INSERT TO authenticated
  WITH CHECK (uploaded_by = auth.uid() AND public.has_task_access(task_id, auth.uid()));
CREATE POLICY task_attachments_delete ON public.task_attachments FOR DELETE TO authenticated
  USING (public.has_task_access(task_id, auth.uid()));

CREATE INDEX task_attachments_task_id_idx ON public.task_attachments(task_id);