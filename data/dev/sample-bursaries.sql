-- Dev-only sample data for scripts/visual-check.mjs. Idempotent: fixed UUIDs + ON CONFLICT DO NOTHING.
-- Needs the taxonomy and sources the API seeds on startup (Database:MigrateOnStartup=true).
-- Closing dates are relative to now(): 3 within 14 days, 8 later, 3 closed, 1 unknown.
BEGIN;

INSERT INTO funder (id, name, normalized_name, website_url, created_at, updated_at) VALUES
  ('f0000000-0000-7000-8000-000000000001', 'Sasol', 'sasol', NULL, now(), now()),
  ('f0000000-0000-7000-8000-000000000002', 'Exxaro Resources', 'exxaro resources', NULL, now(), now()),
  ('f0000000-0000-7000-8000-000000000003', 'City of Cape Town', 'city of cape town', NULL, now(), now()),
  ('f0000000-0000-7000-8000-000000000004', 'Investec', 'investec', NULL, now(), now()),
  ('f0000000-0000-7000-8000-000000000005', 'Nedbank', 'nedbank', NULL, now(), now()),
  ('f0000000-0000-7000-8000-000000000006', 'Transnet', 'transnet', NULL, now(), now()),
  ('f0000000-0000-7000-8000-000000000007', 'Eskom', 'eskom', NULL, now(), now()),
  ('f0000000-0000-7000-8000-000000000008', 'SAICA Thuthuka', 'saica thuthuka', NULL, now(), now()),
  ('f0000000-0000-7000-8000-000000000009', 'Anglo American', 'anglo american', NULL, now(), now()),
  ('f0000000-0000-7000-8000-000000000010', 'Department of Health Western Cape', 'department of health western cape', NULL, now(), now()),
  ('f0000000-0000-7000-8000-000000000011', 'Standard Bank', 'standard bank', NULL, now(), now()),
  ('f0000000-0000-7000-8000-000000000012', 'The Dr Johannes Hendrikus van der Merwe and Sons Educational Foundation for Rural Engineering and Agricultural Students', 'the dr johannes hendrikus van der merwe and sons educational foundation for rural engineering and agricultural students', NULL, now(), now())
ON CONFLICT DO NOTHING;

INSERT INTO bursary (id, slug, name, normalized_name, funder_id, summary, value_summary, coverage, study_levels, min_average, citizenship, provinces, all_fields_eligible, closing_date, status, apply_url, extraction_confidence, locked_fields, is_published, last_seen_at, created_at, updated_at) VALUES
  ('b0000000-0000-7000-8000-000000000001', 'sasol-bursary', 'Sasol Bursary', 'sasol bursary', 'f0000000-0000-7000-8000-000000000001', 'Full-cost bursary for engineering and science students at South African universities.', 'Tuition, accommodation, books and a living allowance', ARRAY['Tuition','Accommodation','Books','Stipend']::text[], ARRAY['Undergraduate']::text[], 65, 'SouthAfrican', ARRAY[]::text[], false, current_date + 6, 'Open', 'https://www.sasol.com/careers/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000002', 'exxaro-bursary', 'Exxaro Bursary Programme', 'exxaro bursary programme', 'f0000000-0000-7000-8000-000000000002', 'Mining, engineering and geoscience bursary with vacation work.', 'Tuition, books, accommodation', ARRAY['Tuition','Books','Accommodation']::text[], ARRAY['Undergraduate','Honours']::text[], 60, 'SouthAfrican', ARRAY[]::text[], false, current_date + 11, 'Open', 'https://www.exxaro.com/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000003', 'city-of-cape-town-bursary', 'City of Cape Town Bursary', 'city of cape town bursary', 'f0000000-0000-7000-8000-000000000003', 'Bursary for Cape Town residents studying scarce-skill qualifications.', 'Tuition and a book allowance', ARRAY['Tuition','Books']::text[], ARRAY['Undergraduate']::text[], 60, 'SouthAfrican', ARRAY[]::text[], false, current_date + 13, 'Open', 'https://www.capetown.gov.za/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000004', 'investec-bursary', 'Investec Bursary', 'investec bursary', 'f0000000-0000-7000-8000-000000000004', 'For students in finance, actuarial science and computer science.', 'Tuition, accommodation and a laptop', ARRAY['Tuition','Accommodation','Laptop']::text[], ARRAY['Undergraduate']::text[], 70, 'SouthAfrican', ARRAY[]::text[], false, current_date + 24, 'Open', 'https://www.investec.com/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000005', 'nedbank-bursary', 'Nedbank Bursary', 'nedbank bursary', 'f0000000-0000-7000-8000-000000000005', 'Bursary for commerce and IT students with leadership potential.', 'Tuition and meals', ARRAY['Tuition','Meals']::text[], ARRAY['Undergraduate','Honours']::text[], 65, 'SouthAfrican', ARRAY[]::text[], false, current_date + 35, 'Open', 'https://www.nedbank.co.za/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000006', 'transnet-bursary', 'Transnet Bursary Scheme', 'transnet bursary scheme', 'f0000000-0000-7000-8000-000000000006', 'Rail, port and pipeline engineering bursary with work-back.', 'Full cost of study', ARRAY['Tuition','Accommodation','Books','Transport']::text[], ARRAY['Undergraduate']::text[], 65, 'SouthAfrican', ARRAY[]::text[], false, current_date + 48, 'Open', 'https://www.transnet.net/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000007', 'eskom-bursary', 'Eskom Bursary', 'eskom bursary', 'f0000000-0000-7000-8000-000000000007', 'Electrical, mechanical and chemical engineering bursary.', 'Tuition, books and accommodation', ARRAY['Tuition','Books','Accommodation']::text[], ARRAY['Undergraduate']::text[], 65, 'SouthAfrican', ARRAY[]::text[], false, current_date + 60, 'Open', 'https://www.eskom.co.za/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000008', 'saica-thuthuka-bursary', 'SAICA Thuthuka Bursary', 'saica thuthuka bursary', 'f0000000-0000-7000-8000-000000000008', 'Accounting bursary for aspiring chartered accountants.', 'Tuition, books and a stipend', ARRAY['Tuition','Books','Stipend']::text[], ARRAY['Undergraduate','Honours']::text[], 60, 'SouthAfrican', ARRAY[]::text[], false, current_date + 75, 'Open', 'https://www.saica.org.za/thuthuka', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000009', 'anglo-american-bursary', 'Anglo American Bursary', 'anglo american bursary', 'f0000000-0000-7000-8000-000000000009', 'Geology, mining and metallurgy bursary.', 'Tuition and accommodation', ARRAY['Tuition','Accommodation']::text[], ARRAY['Undergraduate']::text[], 70, 'SouthAfrican', ARRAY[]::text[], false, current_date + 90, 'Open', 'https://www.angloamerican.com/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000010', 'western-cape-health-bursary', 'Western Cape Health Bursary', 'western cape health bursary', 'f0000000-0000-7000-8000-000000000010', 'Nursing and allied health bursary with a work-back period.', 'Tuition and a stipend', ARRAY['Tuition','Stipend']::text[], ARRAY['Undergraduate','Honours']::text[], 55, 'SouthAfrican', ARRAY[]::text[], false, current_date + 110, 'Open', 'https://www.westerncape.gov.za/health-bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000011', 'standard-bank-masters-bursary', 'Standard Bank Masters Bursary', 'standard bank masters bursary', 'f0000000-0000-7000-8000-000000000011', 'Postgraduate funding for data science and economics.', 'Tuition and a living allowance', ARRAY['Tuition','Stipend']::text[], ARRAY['Masters']::text[], 75, 'SouthAfrican', ARRAY[]::text[], false, current_date + 140, 'Open', 'https://www.standardbank.com/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000012', 'exxaro-matric-bursary', 'Exxaro Matric Bursary', 'exxaro matric bursary', 'f0000000-0000-7000-8000-000000000002', 'Applications for this intake have closed.', 'Tuition', ARRAY['Tuition']::text[], ARRAY['Matric']::text[], 70, 'SouthAfrican', ARRAY[]::text[], false, current_date - 20, 'Closed', 'https://www.exxaro.com/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000013', 'sasol-honours-bursary', 'Sasol Honours Bursary', 'sasol honours bursary', 'f0000000-0000-7000-8000-000000000001', 'Applications for this intake have closed.', 'Tuition and books', ARRAY['Tuition','Books']::text[], ARRAY['Honours']::text[], 70, 'SouthAfrican', ARRAY[]::text[], false, current_date - 45, 'Closed', 'https://www.sasol.com/careers/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000014', 'nedbank-it-bursary', 'Nedbank IT Bursary', 'nedbank it bursary', 'f0000000-0000-7000-8000-000000000005', 'Applications for this intake have closed.', 'Tuition', ARRAY['Tuition']::text[], ARRAY['Undergraduate']::text[], 65, 'SouthAfrican', ARRAY[]::text[], false, current_date - 10, 'Closed', 'https://www.nedbank.co.za/bursaries', 0.90, ARRAY[]::text[], true, now(), now(), now()),
  ('b0000000-0000-7000-8000-000000000015', 'van-der-merwe-rural-engineering-and-agriculture-bursary', 'Dr Johannes Hendrikus van der Merwe and Sons Educational Foundation Bursary for Rural Engineering and Agricultural Students', 'dr johannes hendrikus van der merwe and sons educational foundation bursary for rural engineering and agricultural students', 'f0000000-0000-7000-8000-000000000012', 'Closing date not yet announced. This name is deliberately very long to test wrapping.', 'To be confirmed', ARRAY['Tuition']::text[], ARRAY['Undergraduate']::text[], NULL, 'SouthAfrican', ARRAY[]::text[], false, NULL, 'Unknown', 'https://example.org/apply', 0.90, ARRAY[]::text[], true, now(), now(), now())
ON CONFLICT DO NOTHING;

INSERT INTO bursary_field (bursary_id, field_of_study_id)
SELECT v.b::uuid, f.id FROM (VALUES
  ('b0000000-0000-7000-8000-000000000001', 'engineering'),
  ('b0000000-0000-7000-8000-000000000002', 'engineering'),
  ('b0000000-0000-7000-8000-000000000003', 'public-management-and-services'),
  ('b0000000-0000-7000-8000-000000000004', 'business-economics-and-management-studies'),
  ('b0000000-0000-7000-8000-000000000005', 'business-economics-and-management-studies'),
  ('b0000000-0000-7000-8000-000000000006', 'engineering'),
  ('b0000000-0000-7000-8000-000000000007', 'engineering'),
  ('b0000000-0000-7000-8000-000000000008', 'business-economics-and-management-studies'),
  ('b0000000-0000-7000-8000-000000000009', 'physical-sciences'),
  ('b0000000-0000-7000-8000-000000000010', 'health-professions-and-related-clinical-sciences'),
  ('b0000000-0000-7000-8000-000000000011', 'mathematics-and-statistics'),
  ('b0000000-0000-7000-8000-000000000012', 'engineering'),
  ('b0000000-0000-7000-8000-000000000013', 'physical-sciences'),
  ('b0000000-0000-7000-8000-000000000014', 'computer-and-information-sciences'),
  ('b0000000-0000-7000-8000-000000000015', 'agriculture-agricultural-operations-and-related-sciences')
) AS v(b, slug) JOIN field_of_study f ON f.slug = v.slug
ON CONFLICT DO NOTHING;

INSERT INTO bursary_institution (bursary_id, institution_id)
SELECT v.b::uuid, i.id FROM (VALUES
  ('b0000000-0000-7000-8000-000000000001', 'uct'), ('b0000000-0000-7000-8000-000000000001', 'wits'), ('b0000000-0000-7000-8000-000000000001', 'up'),
  ('b0000000-0000-7000-8000-000000000002', 'wits'), ('b0000000-0000-7000-8000-000000000002', 'up'), ('b0000000-0000-7000-8000-000000000002', 'nwu'),
  ('b0000000-0000-7000-8000-000000000003', 'uct'), ('b0000000-0000-7000-8000-000000000003', 'su'),
  ('b0000000-0000-7000-8000-000000000004', 'uct'), ('b0000000-0000-7000-8000-000000000004', 'wits'), ('b0000000-0000-7000-8000-000000000004', 'su'),
  ('b0000000-0000-7000-8000-000000000005', 'wits'), ('b0000000-0000-7000-8000-000000000005', 'up'), ('b0000000-0000-7000-8000-000000000005', 'ukzn'),
  ('b0000000-0000-7000-8000-000000000006', 'up'), ('b0000000-0000-7000-8000-000000000006', 'ukzn'), ('b0000000-0000-7000-8000-000000000006', 'nwu'),
  ('b0000000-0000-7000-8000-000000000007', 'uct'), ('b0000000-0000-7000-8000-000000000007', 'wits'), ('b0000000-0000-7000-8000-000000000007', 'up'), ('b0000000-0000-7000-8000-000000000007', 'su'),
  ('b0000000-0000-7000-8000-000000000008', 'wits'), ('b0000000-0000-7000-8000-000000000008', 'up'), ('b0000000-0000-7000-8000-000000000008', 'ukzn'), ('b0000000-0000-7000-8000-000000000008', 'nwu'),
  ('b0000000-0000-7000-8000-000000000009', 'wits'), ('b0000000-0000-7000-8000-000000000009', 'up'),
  ('b0000000-0000-7000-8000-000000000010', 'uct'), ('b0000000-0000-7000-8000-000000000010', 'su'),
  ('b0000000-0000-7000-8000-000000000011', 'uct'), ('b0000000-0000-7000-8000-000000000011', 'wits'),
  ('b0000000-0000-7000-8000-000000000012', 'wits'),
  ('b0000000-0000-7000-8000-000000000013', 'up'),
  ('b0000000-0000-7000-8000-000000000014', 'uct'),
  ('b0000000-0000-7000-8000-000000000015', 'ufs'), ('b0000000-0000-7000-8000-000000000015', 'ru')
) AS v(b, slug) JOIN institution i ON i.slug = v.slug
ON CONFLICT DO NOTHING;

INSERT INTO bursary_source (bursary_id, source_id, source_url, first_seen_at, last_seen_at)
SELECT v.b::uuid, s.id, v.url, now(), now() FROM (VALUES
  ('b0000000-0000-7000-8000-000000000001', 'zabursaries', 'https://www.zabursaries.co.za/engineering-bursaries/sasol-bursary/'),
  ('b0000000-0000-7000-8000-000000000001', 'studytrust', 'https://studytrust.org.za/bursaries/sasol-bursary/'),
  ('b0000000-0000-7000-8000-000000000001', 'allbursaries', 'https://allbursaries.co.za/sasol-bursary/'),
  ('b0000000-0000-7000-8000-000000000002', 'zabursaries', 'https://www.zabursaries.co.za/mining-bursaries/exxaro-bursary/'),
  ('b0000000-0000-7000-8000-000000000002', 'gocareers', 'https://gocareers.co.za/bursaries/exxaro-bursary'),
  ('b0000000-0000-7000-8000-000000000003', 'zabursaries', 'https://www.zabursaries.co.za/government-bursaries-south-africa/city-of-cape-town-bursary/'),
  ('b0000000-0000-7000-8000-000000000003', 'studytrust', 'https://studytrust.org.za/bursaries/city-of-cape-town-bursary/'),
  ('b0000000-0000-7000-8000-000000000003', 'fundiconnect', 'https://fundiconnect.co.za/bursaries/city-of-cape-town'),
  ('b0000000-0000-7000-8000-000000000004', 'zabursaries', 'https://www.zabursaries.co.za/finance-bursaries/investec-bursary/'),
  ('b0000000-0000-7000-8000-000000000004', 'careerdrive', 'https://careerdrive.co.za/investec-bursary'),
  ('b0000000-0000-7000-8000-000000000005', 'allbursaries', 'https://allbursaries.co.za/nedbank-bursary/'),
  ('b0000000-0000-7000-8000-000000000006', 'zabursaries', 'https://www.zabursaries.co.za/engineering-bursaries/transnet-bursary/'),
  ('b0000000-0000-7000-8000-000000000006', 'bursaryza', 'https://bursaryza.co.za/transnet-bursary/'),
  ('b0000000-0000-7000-8000-000000000006', 'edupstairs', 'https://edupstairs.org/transnet-bursary/'),
  ('b0000000-0000-7000-8000-000000000007', 'zabursaries', 'https://www.zabursaries.co.za/engineering-bursaries/eskom-bursary/'),
  ('b0000000-0000-7000-8000-000000000007', 'studytrust', 'https://studytrust.org.za/bursaries/eskom-bursary/'),
  ('b0000000-0000-7000-8000-000000000015', 'allbursaries', 'https://allbursaries.co.za/rural-engineering-and-agriculture-bursary/')
) AS v(b, key, url) JOIN source s ON s.key = v.key
ON CONFLICT DO NOTHING;

COMMIT;
