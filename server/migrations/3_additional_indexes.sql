create index if not exists users_name_world_idx on users (name, world);
create index if not exists users_key_short_key_hash_idx on users (key_short, key_hash);
create index if not exists channel_invites_invited_idx on channel_invites (invited);
